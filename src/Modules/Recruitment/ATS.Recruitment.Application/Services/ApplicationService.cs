using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Application.Ports;
using ATS.Recruitment.Domain;
using ATS.Recruitment.Domain.Ports;
using ATS.SharedKernel;
using Microsoft.Extensions.Logging;
using JobApplication = ATS.Recruitment.Domain.Application;

namespace ATS.Recruitment.Application.Services;

/// <summary>Nop don (UC-02), lich su ung tuyen, danh sach xep hang cho HR, chuyen trang thai.</summary>
public sealed class ApplicationService : IApplicationService
{
    /// <summary>Gia tri ScreeningStatus khi AiScreening chua co ban ghi nao cho don (RB9).</summary>
    public const string PendingScreeningStatus = "Pending";

    private readonly IApplicationRepository _applications;
    private readonly IJobRepository _jobs;
    private readonly ICandidateRepository _candidates;
    private readonly ICvRepository _cvs;
    private readonly IFileStorage _storage;
    private readonly IApplicationScreeningTrigger _screening;
    private readonly IApplicationScoreReader _scores;
    private readonly IUserDirectory _userDirectory;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _clock;
    private readonly ILogger<ApplicationService> _logger;
    private readonly ApplicationAccess _access;

    public ApplicationService(
        IApplicationRepository applications,
        IJobRepository jobs,
        ICandidateRepository candidates,
        ICvRepository cvs,
        IFileStorage storage,
        IApplicationScreeningTrigger screening,
        IApplicationScoreReader scores,
        IUserDirectory userDirectory,
        IUnitOfWork uow,
        TimeProvider clock,
        ILogger<ApplicationService> logger)
    {
        _applications = applications;
        _jobs = jobs;
        _candidates = candidates;
        _cvs = cvs;
        _storage = storage;
        _screening = screening;
        _scores = scores;
        _userDirectory = userDirectory;
        _uow = uow;
        _clock = clock;
        _logger = logger;
        _access = new ApplicationAccess(applications, jobs, candidates);
    }

    public async Task<Result<ApplicationDto>> ApplyAsync(Guid candidateUserId, ApplyRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var candidate = await _candidates.GetByUserIdAsync(candidateUserId, ct);
        if (candidate is null)
        {
            return Result.Failure<ApplicationDto>(Errors.CandidateProfileMissing);
        }

        // Rang buoc 1 (database-design muc 3): CV phai thuoc dung ung vien nop don.
        var cv = await _cvs.GetByIdAsync(request.CvId, ct);
        if (cv is null || cv.CandidateId != candidate.Id)
        {
            return Result.Failure<ApplicationDto>(Error.NotFound("Khong tim thay CV."));
        }

        var job = await _jobs.GetByIdAsync(request.JobId, ct);
        if (job is null)
        {
            return Result.Failure<ApplicationDto>(Error.NotFound("Khong tim thay tin tuyen dung."));
        }

        if (!job.IsOpen)
        {
            return Result.Failure<ApplicationDto>(Error.Validation("Tin nay khong con nhan ho so."));
        }

        if (await _applications.ExistsAsync(job.Id, candidate.Id, ct))
        {
            return Result.Failure<ApplicationDto>(Error.Conflict("Ban da nop don vao tin nay."));
        }

        var application = JobApplication.Submit(job.Id, candidate.Id, cv.Id, _clock.GetUtcNow());
        await _applications.AddAsync(application, ct);
        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            // Hai request nop CUNG luc deu qua ExistsAsync o tren; unique (job_id, candidate_id)
            // trong database chan request thu hai. Tra 409 nhu nop trung binh thuong, khong 500.
            return Result.Failure<ApplicationDto>(Error.Conflict("Ban da nop don vao tin nay."));
        }

        // Don DA duoc luu. Nho AiScreening cham la viec phu: loi o day (Redis sap...) KHONG duoc
        // lam hong viec nop don — don van hien voi ScreeningStatus "Pending" (RB9).
        try
        {
            await _screening.RequestScreeningAsync(application.Id, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Khong yeu cau duoc cham AI cho don {ApplicationId}.", application.Id);
        }

        return ToDto(application, job, cv, score: null);
    }

    public async Task<Result<IReadOnlyList<ApplicationDto>>> ListMineAsync(Guid candidateUserId, CancellationToken ct = default)
    {
        var candidate = await _candidates.GetByUserIdAsync(candidateUserId, ct);
        if (candidate is null)
        {
            return Result.Failure<IReadOnlyList<ApplicationDto>>(Errors.CandidateProfileMissing);
        }

        var applications = await _applications.ListByCandidateAsync(candidate.Id, ct);
        if (applications.Count == 0)
        {
            return Result.Success<IReadOnlyList<ApplicationDto>>([]);
        }

        var jobs = (await _jobs.GetByIdsAsync(applications.Select(a => a.JobId).Distinct().ToList(), ct)).ToDictionary(j => j.Id);
        var cvs = (await _cvs.GetByIdsAsync(applications.Select(a => a.CvId).Distinct().ToList(), ct)).ToDictionary(c => c.Id);
        var scores = await ReadScoresAsync(applications, ct);

        IReadOnlyList<ApplicationDto> dtos = applications
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => ToDto(a, jobs.GetValueOrDefault(a.JobId), cvs.GetValueOrDefault(a.CvId), scores.GetValueOrDefault(a.Id)))
            .ToList();
        return Result.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<RankedApplicationDto>>> ListForJobAsync(Guid hrUserId, Guid jobId, string? status, CancellationToken ct = default)
    {
        ApplicationStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseStatus(status, out var parsed))
            {
                return Result.Failure<IReadOnlyList<RankedApplicationDto>>(StatusError);
            }

            statusFilter = parsed;
        }

        var job = await _jobs.GetByIdAsync(jobId, ct);
        if (job is null)
        {
            return Result.Failure<IReadOnlyList<RankedApplicationDto>>(Error.NotFound("Khong tim thay tin tuyen dung."));
        }

        if (job.HrOwnerId != hrUserId)
        {
            return Result.Failure<IReadOnlyList<RankedApplicationDto>>(Error.Forbidden("Ban khong so huu tin nay."));
        }

        var applications = await _applications.ListByJobAsync(jobId, statusFilter, ct);
        if (applications.Count == 0)
        {
            return Result.Success<IReadOnlyList<RankedApplicationDto>>([]);
        }

        var candidates = (await _candidates.GetByIdsAsync(applications.Select(a => a.CandidateId).Distinct().ToList(), ct)).ToDictionary(c => c.Id);
        var cvs = (await _cvs.GetByIdsAsync(applications.Select(a => a.CvId).Distinct().ToList(), ct)).ToDictionary(c => c.Id);
        var scores = await ReadScoresAsync(applications, ct);

        // Diem giam dan, don chua cham / Failed (Score null) o CUOI nhung VAN hien (RB9).
        IReadOnlyList<RankedApplicationDto> ranked = applications
            .Select(a =>
            {
                var score = scores.GetValueOrDefault(a.Id);
                return new RankedApplicationDto(
                    a.Id,
                    candidates.GetValueOrDefault(a.CandidateId)?.FullName ?? string.Empty,
                    a.CvId,
                    cvs.GetValueOrDefault(a.CvId)?.Name ?? string.Empty,
                    a.Status.ToString(),
                    a.AppliedAt,
                    score?.Score,
                    score?.Summary,
                    score?.AdapterUsed,
                    score?.ScreeningStatus ?? PendingScreeningStatus);
            })
            .OrderBy(r => r.AiScore is null)
            .ThenByDescending(r => r.AiScore)
            .ThenBy(r => r.AppliedAt)
            .ToList();
        return Result.Success(ranked);
    }

    public async Task<Result<ApplicationDetailDto>> GetDetailAsync(Guid callerUserId, Guid applicationId, CancellationToken ct = default)
    {
        var access = await _access.LoadAsync(callerUserId, applicationId, hrOwnerOnly: false, ct);
        if (access.IsFailure)
        {
            return Result.Failure<ApplicationDetailDto>(access.Error);
        }

        var (application, job, candidate, isHrOwner) = access.Value;
        var cv = await _cvs.GetByIdAsync(application.CvId, ct);
        var score = (await ReadScoresAsync([application], ct)).GetValueOrDefault(application.Id);
        var email = await _userDirectory.GetEmailAsync(candidate.UserId, ct);

        return new ApplicationDetailDto(
            application.Id,
            job.Id,
            job.Title,
            candidate.FullName,
            email,
            application.CvId,
            cv?.Name ?? string.Empty,
            application.Status.ToString(),
            isHrOwner ? application.HrNote : null,
            application.AppliedAt,
            application.UpdatedAt,
            score?.Score,
            score?.Summary,
            score?.AdapterUsed,
            score?.ScreeningStatus ?? PendingScreeningStatus);
    }

    public async Task<Result<CvFile>> GetCvFileAsync(Guid callerUserId, Guid applicationId, CancellationToken ct = default)
    {
        var access = await _access.LoadAsync(callerUserId, applicationId, hrOwnerOnly: false, ct);
        if (access.IsFailure)
        {
            return Result.Failure<CvFile>(access.Error);
        }

        var cv = await _cvs.GetByIdAsync(access.Value.Application.CvId, ct);
        if (cv is null)
        {
            return Result.Failure<CvFile>(Error.NotFound("Khong tim thay CV."));
        }

        Stream content;
        try
        {
            content = await _storage.ReadAsync(cv.FilePath, ct);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return Result.Failure<CvFile>(Error.NotFound("File CV khong con tren may chu."));
        }

        var contentType = cv.FileType == "pdf"
            ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        return new CvFile(content, contentType, $"{SafeFileName(cv.Name)}.{cv.FileType}");
    }

    public async Task<Result> ChangeStatusAsync(Guid hrUserId, Guid applicationId, ChangeStatusRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryParseStatus(request.Status, out var target))
        {
            return Result.Failure(StatusError);
        }

        var access = await _access.LoadAsync(hrUserId, applicationId, hrOwnerOnly: true, ct);
        if (access.IsFailure)
        {
            return access;
        }

        var changed = access.Value.Application.ChangeStatus(target, request.Note, _clock.GetUtcNow());
        if (changed.IsFailure)
        {
            return changed;
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static readonly Error StatusError =
        Error.Validation("Trang thai phai la Applied, Screening, Interview, Hired hoac Rejected.");

    private static bool TryParseStatus(string? value, out ApplicationStatus status)
        => Enum.TryParse(value?.Trim(), ignoreCase: true, out status)
           && Enum.IsDefined(status)
           && !int.TryParse(value, out _);

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) || ch == '"' ? '_' : ch).ToArray()).Trim();
        return cleaned.Length == 0 ? "cv" : cleaned;
    }

    private static ApplicationDto ToDto(JobApplication a, Job? job, Cv? cv, ApplicationAiScore? score) => new(
        a.Id,
        a.JobId,
        job?.Title ?? string.Empty,
        a.CvId,
        cv?.Name ?? string.Empty,
        a.Status.ToString(),
        a.AppliedAt,
        score?.Score,
        score?.Summary,
        score?.AdapterUsed,
        score?.ScreeningStatus ?? PendingScreeningStatus);

    private async Task<IReadOnlyDictionary<Guid, ApplicationAiScore>> ReadScoresAsync(IReadOnlyList<JobApplication> applications, CancellationToken ct)
    {
        try
        {
            return await _scores.GetScoresAsync(applications.Select(a => a.Id).ToList(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Thieu diem AI khong bao gio chan HR/ung vien xem don (RB9): coi nhu tat ca "Pending".
            _logger.LogWarning(ex, "Khong doc duoc diem AI; hien danh sach khong diem.");
            return new Dictionary<Guid, ApplicationAiScore>();
        }
    }
}
