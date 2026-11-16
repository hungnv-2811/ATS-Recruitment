using System.Globalization;
using System.Net;
using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Application.Ports;
using ATS.Recruitment.Domain;
using ATS.Recruitment.Domain.Ports;
using ATS.SharedKernel;
using ATS.SharedKernel.Ports;
using Microsoft.Extensions.Logging;

namespace ATS.Recruitment.Application.Services;

/// <summary>Hen phong van, gui/gui lai loi moi, nhap danh gia (UC-04, tuan 8).</summary>
public sealed class InterviewService : IInterviewService
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    private readonly IInterviewRepository _interviews;
    private readonly IEvaluationRepository _evaluations;
    private readonly IApplicationRepository _applications;
    private readonly IJobRepository _jobs;
    private readonly ICandidateRepository _candidates;
    private readonly IUserDirectory _userDirectory;
    private readonly IEmailSender _email;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _clock;
    private readonly ILogger<InterviewService> _logger;
    private readonly ApplicationAccess _access;

    public InterviewService(
        IInterviewRepository interviews,
        IEvaluationRepository evaluations,
        IApplicationRepository applications,
        IJobRepository jobs,
        ICandidateRepository candidates,
        IUserDirectory userDirectory,
        IEmailSender email,
        IUnitOfWork uow,
        TimeProvider clock,
        ILogger<InterviewService> logger)
    {
        _interviews = interviews;
        _evaluations = evaluations;
        _applications = applications;
        _jobs = jobs;
        _candidates = candidates;
        _userDirectory = userDirectory;
        _email = email;
        _uow = uow;
        _clock = clock;
        _logger = logger;
        _access = new ApplicationAccess(applications, jobs, candidates);
    }

    public async Task<Result<InterviewDto>> ScheduleAsync(Guid hrUserId, Guid applicationId, ScheduleInterviewRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = await _access.LoadAsync(hrUserId, applicationId, hrOwnerOnly: true, ct);
        if (access.IsFailure)
        {
            return Result.Failure<InterviewDto>(access.Error);
        }

        var context = access.Value;
        var now = _clock.GetUtcNow();

        var scheduled = Interview.Schedule(
            context.Application.Id, request.ScheduledAt, request.Mode, request.LocationOrLink, request.InterviewerName, now);
        if (scheduled.IsFailure)
        {
            return Result.Failure<InterviewDto>(scheduled.Error);
        }

        // Don Applied/Screening tu sang Interview; don da Hired/Rejected thi khong hen duoc.
        var stage = context.Application.EnterInterviewStage(now);
        if (stage.IsFailure)
        {
            return Result.Failure<InterviewDto>(stage.Error);
        }

        var interview = scheduled.Value;
        await _interviews.AddAsync(interview, ct);

        // 1) LUU TRUOC — buoi phong van ton tai bat ke email ra sao.
        await _uow.SaveChangesAsync(ct);

        // 2) Gui mail SAU va NGOAI transaction; ket qua chi cap nhat co EmailSent.
        var sent = await TrySendInviteAsync(interview, context.Job, context.Candidate, ct);
        if (sent)
        {
            interview.MarkEmailSent(true);
            await _uow.SaveChangesAsync(ct);
        }

        return ToDto(interview, evaluation: null);
    }

    public async Task<Result<IReadOnlyList<InterviewDto>>> ListByApplicationAsync(Guid callerUserId, Guid applicationId, CancellationToken ct = default)
    {
        var access = await _access.LoadAsync(callerUserId, applicationId, hrOwnerOnly: false, ct);
        if (access.IsFailure)
        {
            return Result.Failure<IReadOnlyList<InterviewDto>>(access.Error);
        }

        var interviews = await _interviews.ListByApplicationAsync(applicationId, ct);
        var evaluations = interviews.Count == 0
            ? new Dictionary<Guid, Evaluation>()
            : (await _evaluations.ListByInterviewsAsync(interviews.Select(i => i.Id).ToList(), ct)).ToDictionary(e => e.InterviewId);

        IReadOnlyList<InterviewDto> dtos = interviews
            .OrderBy(i => i.ScheduledAt)
            .Select(i => ToDto(i, evaluations.GetValueOrDefault(i.Id)))
            .ToList();
        return Result.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<InterviewListItemDto>>> ListUpcomingForHrAsync(Guid hrUserId, CancellationToken ct = default)
    {
        // Ca buoi da bat dau trong ngay hom nay (UTC) van hien, de HR nhap danh gia ngay sau buoi.
        var from = new DateTimeOffset(_clock.GetUtcNow().UtcDateTime.Date, TimeSpan.Zero);
        var interviews = await _interviews.ListUpcomingForHrAsync(hrUserId, from, ct);
        if (interviews.Count == 0)
        {
            return Result.Success<IReadOnlyList<InterviewListItemDto>>([]);
        }

        var applications = (await _applications.GetByIdsAsync(interviews.Select(i => i.ApplicationId).Distinct().ToList(), ct)).ToDictionary(a => a.Id);
        var jobs = (await _jobs.GetByIdsAsync(applications.Values.Select(a => a.JobId).Distinct().ToList(), ct)).ToDictionary(j => j.Id);
        var candidates = (await _candidates.GetByIdsAsync(applications.Values.Select(a => a.CandidateId).Distinct().ToList(), ct)).ToDictionary(c => c.Id);

        IReadOnlyList<InterviewListItemDto> items = interviews
            .OrderBy(i => i.ScheduledAt)
            .Select(i =>
            {
                var application = applications.GetValueOrDefault(i.ApplicationId);
                return new InterviewListItemDto(
                    i.Id,
                    i.ApplicationId,
                    application is null ? string.Empty : jobs.GetValueOrDefault(application.JobId)?.Title ?? string.Empty,
                    application is null ? string.Empty : candidates.GetValueOrDefault(application.CandidateId)?.FullName ?? string.Empty,
                    i.ScheduledAt,
                    i.Mode,
                    i.Status.ToString(),
                    i.EmailSent);
            })
            .ToList();
        return Result.Success(items);
    }

    public async Task<Result> ResendInviteAsync(Guid hrUserId, Guid interviewId, CancellationToken ct = default)
    {
        var loaded = await LoadOwnedInterviewAsync(hrUserId, interviewId, ct);
        if (loaded.IsFailure)
        {
            return loaded;
        }

        var (interview, context) = loaded.Value;
        if (interview.Status != InterviewStatus.Scheduled)
        {
            return Result.Failure(Error.Validation("Chi gui lai loi moi cho buoi phong van chua dien ra."));
        }

        if (!await TrySendInviteAsync(interview, context.Job, context.Candidate, ct))
        {
            return Result.Failure(Error.External("Khong gui duoc email moi phong van. Thu lai sau."));
        }

        interview.MarkEmailSent(true);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<EvaluationDto>> SubmitEvaluationAsync(Guid hrUserId, Guid interviewId, SubmitEvaluationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.TryParse<Recommendation>(request.Recommendation?.Trim(), ignoreCase: true, out var recommendation)
            || !Enum.IsDefined(recommendation)
            || int.TryParse(request.Recommendation, out _))
        {
            return Result.Failure<EvaluationDto>(Error.Validation("De xuat phai la Hire, Reject hoac Consider."));
        }

        var loaded = await LoadOwnedInterviewAsync(hrUserId, interviewId, ct);
        if (loaded.IsFailure)
        {
            return Result.Failure<EvaluationDto>(loaded.Error);
        }

        var interview = loaded.Value.Interview;
        if (await _evaluations.ExistsForInterviewAsync(interview.Id, ct))
        {
            return Result.Failure<EvaluationDto>(Error.Conflict("Buoi phong van nay da co danh gia."));
        }

        var created = Evaluation.Create(interview.Id, request.Rating, request.Comment, recommendation, _clock.GetUtcNow());
        if (created.IsFailure)
        {
            return Result.Failure<EvaluationDto>(created.Error);
        }

        var completed = interview.Complete();
        if (completed.IsFailure)
        {
            return Result.Failure<EvaluationDto>(completed.Error);
        }

        await _evaluations.AddAsync(created.Value, ct);
        await _uow.SaveChangesAsync(ct);
        return ToDto(created.Value);
    }

    internal static InterviewDto ToDto(Interview i, Evaluation? evaluation) => new(
        i.Id,
        i.ApplicationId,
        i.ScheduledAt,
        i.Mode,
        i.LocationOrLink,
        i.InterviewerName,
        i.Status.ToString(),
        i.EmailSent,
        evaluation is null ? null : ToDto(evaluation));

    private static EvaluationDto ToDto(Evaluation e)
        => new(e.Id, e.Rating, e.Comment, e.Recommendation.ToString(), e.CreatedAt);

    private async Task<Result<(Interview Interview, ApplicationContext Context)>> LoadOwnedInterviewAsync(Guid hrUserId, Guid interviewId, CancellationToken ct)
    {
        var interview = await _interviews.GetByIdAsync(interviewId, ct);
        if (interview is null)
        {
            return Result.Failure<(Interview, ApplicationContext)>(Errors.InterviewNotFound);
        }

        var access = await _access.LoadAsync(hrUserId, interview.ApplicationId, hrOwnerOnly: true, ct);
        return access.IsFailure
            ? Result.Failure<(Interview, ApplicationContext)>(access.Error)
            : (interview, access.Value);
    }

    /// <summary>Gui loi moi; MOI loi (SMTP sap, thieu email...) chi ghi log va tra false.</summary>
    private async Task<bool> TrySendInviteAsync(Interview interview, Job job, Candidate candidate, CancellationToken ct)
    {
        try
        {
            var to = await _userDirectory.GetEmailAsync(candidate.UserId, ct);
            if (string.IsNullOrWhiteSpace(to))
            {
                _logger.LogWarning("Khong tim thay email cua ung vien cho buoi phong van {InterviewId}.", interview.Id);
                return false;
            }

            // Ung vien o Viet Nam: hien gio +07 thay vi UTC tho. Offset co dinh (VN khong doi gio
            // mua he) nen khong phu thuoc tzdata cua container.
            var localTime = interview.ScheduledAt.ToOffset(VietnamOffset);
            var when = localTime.ToString("HH:mm 'ngày' dd/MM/yyyy", CultureInfo.InvariantCulture) + " (giờ Việt Nam)";
            var isOnline = interview.Mode == Interview.ModeOnline;
            var mode = isOnline ? "Trực tuyến" : "Tại văn phòng";
            var where = isOnline ? "Link phỏng vấn" : "Địa điểm";
            var html =
                $"<p>Chào {WebUtility.HtmlEncode(candidate.FullName)},</p>" +
                $"<p>Bạn được mời phỏng vấn cho vị trí <strong>{WebUtility.HtmlEncode(job.Title)}</strong>.</p>" +
                "<ul>" +
                $"<li>Thời gian: {when}</li>" +
                $"<li>Hình thức: {mode}</li>" +
                $"<li>{where}: {WebUtility.HtmlEncode(interview.LocationOrLink)}</li>" +
                $"<li>Người phỏng vấn: {WebUtility.HtmlEncode(interview.InterviewerName)}</li>" +
                "</ul>" +
                "<p>Chúc bạn phỏng vấn thành công!</p>";
            var text =
                $"Mời phỏng vấn vị trí {job.Title}. Thời gian: {when}. Hình thức: {mode}. " +
                $"{where}: {interview.LocationOrLink}. Người phỏng vấn: {interview.InterviewerName}.";

            await _email.SendAsync(new EmailMessage(to, $"Mời phỏng vấn: {job.Title}", html, text), ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Gui loi moi phong van {InterviewId} that bai; EmailSent=false.", interview.Id);
            return false;
        }
    }
}
