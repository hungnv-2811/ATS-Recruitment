using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Domain;
using ATS.Recruitment.Domain.Ports;
using ATS.SharedKernel;
using Microsoft.Extensions.Logging;

namespace ATS.Recruitment.Application.Services;

/// <summary>Quan ly nhieu CV cua ung vien (UC-01).</summary>
public sealed class CvService : ICvService
{
    private readonly ICandidateRepository _candidates;
    private readonly ICvRepository _cvs;
    private readonly IApplicationRepository _applications;
    private readonly IFileStorage _storage;
    private readonly ICvTextExtractor _extractor;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _clock;
    private readonly ILogger<CvService> _logger;

    public CvService(
        ICandidateRepository candidates,
        ICvRepository cvs,
        IApplicationRepository applications,
        IFileStorage storage,
        ICvTextExtractor extractor,
        IUnitOfWork uow,
        TimeProvider clock,
        ILogger<CvService> logger)
    {
        _candidates = candidates;
        _cvs = cvs;
        _applications = applications;
        _storage = storage;
        _extractor = extractor;
        _uow = uow;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<CvDto>>> ListAsync(Guid candidateUserId, CancellationToken ct = default)
    {
        var candidate = await _candidates.GetByUserIdAsync(candidateUserId, ct);
        if (candidate is null)
        {
            return Result.Failure<IReadOnlyList<CvDto>>(Errors.CandidateProfileMissing);
        }

        var cvs = await _cvs.ListByCandidateAsync(candidate.Id, ct);
        return Result.Success<IReadOnlyList<CvDto>>(cvs.Select(ToDto).ToList());
    }

    public async Task<Result<CvDto>> UploadAsync(Guid candidateUserId, UploadCvCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > Cv.NameMaxLength)
        {
            return Result.Failure<CvDto>(Error.Validation($"Ten CV bat buoc, toi da {Cv.NameMaxLength} ky tu."));
        }

        var fileType = Cv.NormalizeFileType(command.FileName);
        if (fileType is null)
        {
            return Result.Failure<CvDto>(Error.Validation("Chi chap nhan file PDF hoac DOCX."));
        }

        if (command.Length <= 0)
        {
            return Result.Failure<CvDto>(Error.Validation("File rong."));
        }

        if (command.Length > Cv.MaxFileSizeBytes)
        {
            return Result.Failure<CvDto>(Error.Validation("File vuot qua 5 MB."));
        }

        var candidate = await _candidates.GetByUserIdAsync(candidateUserId, ct);
        if (candidate is null)
        {
            return Result.Failure<CvDto>(Errors.CandidateProfileMissing);
        }

        // Doc vao bo nho MOT lan (<= 5 MB): luu file va trich text deu can doc lai tu dau.
        using var buffer = new MemoryStream();
        await command.Content.CopyToAsync(buffer, ct);
        if (buffer.Length > Cv.MaxFileSizeBytes)
        {
            return Result.Failure<CvDto>(Error.Validation("File vuot qua 5 MB."));
        }

        buffer.Position = 0;
        var path = await _storage.SaveAsync(buffer, $"cv.{fileType}", ct);

        buffer.Position = 0;
        var text = await ExtractTextAsync(buffer, fileType, ct);

        var existing = await _cvs.ListByCandidateAsync(candidate.Id, ct);
        var cv = Cv.Create(candidate.Id, command.Name, path, fileType, (int)buffer.Length, text, _clock.GetUtcNow());
        if (!existing.Any(c => c.IsDefault))
        {
            cv.MarkDefault();
        }

        try
        {
            await _cvs.AddAsync(cv, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch
        {
            await TryDeleteFileAsync(path);
            throw;
        }

        return ToDto(cv);
    }

    public async Task<Result> DeleteAsync(Guid candidateUserId, Guid cvId, CancellationToken ct = default)
    {
        var owned = await GetOwnedAsync(candidateUserId, cvId, ct);
        if (owned.IsFailure)
        {
            return owned;
        }

        var cv = owned.Value;
        if (await _applications.AnyUsingCvAsync(cv.Id, ct))
        {
            return Result.Failure(Error.Conflict("CV nay da duoc dung de nop don, khong xoa duoc."));
        }

        var wasDefault = cv.IsDefault;
        await _cvs.DeleteAsync(cv, ct);
        await _uow.SaveChangesAsync(ct);

        // Con CV khac thi chuyen mac dinh sang CV moi nhat. Hai lan SaveChanges co chu dich:
        // khong de partial unique index (is_default) phu thuoc thu tu lenh cua EF.
        if (wasDefault)
        {
            var next = (await _cvs.ListByCandidateAsync(cv.CandidateId, ct)).MaxBy(c => c.UploadedAt);
            if (next is not null)
            {
                next.MarkDefault();
                await _uow.SaveChangesAsync(ct);
            }
        }

        await TryDeleteFileAsync(cv.FilePath);
        return Result.Success();
    }

    public async Task<Result> SetDefaultAsync(Guid candidateUserId, Guid cvId, CancellationToken ct = default)
    {
        var owned = await GetOwnedAsync(candidateUserId, cvId, ct);
        if (owned.IsFailure)
        {
            return owned;
        }

        var cv = owned.Value;
        if (cv.IsDefault)
        {
            return Result.Success();
        }

        var current = (await _cvs.ListByCandidateAsync(cv.CandidateId, ct)).Where(c => c.IsDefault).ToList();
        if (current.Count > 0)
        {
            current.ForEach(c => c.UnmarkDefault());
            await _uow.SaveChangesAsync(ct);
        }

        cv.MarkDefault();
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    internal static CvDto ToDto(Cv cv) => new(
        cv.Id,
        cv.Name,
        cv.FileType,
        (int)Math.Ceiling(cv.FileSize / 1024d),
        cv.IsDefault,
        cv.UploadedAt,
        cv.HasText);

    private async Task<Result<Cv>> GetOwnedAsync(Guid candidateUserId, Guid cvId, CancellationToken ct)
    {
        var candidate = await _candidates.GetByUserIdAsync(candidateUserId, ct);
        if (candidate is null)
        {
            return Result.Failure<Cv>(Errors.CandidateProfileMissing);
        }

        var cv = await _cvs.GetByIdAsync(cvId, ct);

        // CV cua nguoi khac: tra NotFound, khong xac nhan la CV do ton tai.
        return cv is null || cv.CandidateId != candidate.Id
            ? Result.Failure<Cv>(Error.NotFound("Khong tim thay CV."))
            : cv;
    }

    private async Task<string> ExtractTextAsync(Stream content, string fileType, CancellationToken ct)
    {
        try
        {
            return await _extractor.ExtractAsync(content, fileType, ct) ?? string.Empty;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Khong doc duoc chu van cho upload (HasText=false) — weekly-plan, bang rui ro.
            _logger.LogWarning(ex, "Khong trich duoc text tu CV ({FileType}).", fileType);
            return string.Empty;
        }
    }

    private async Task TryDeleteFileAsync(string path)
    {
        try
        {
            await _storage.DeleteAsync(path, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong xoa duoc file CV {Path}.", path);
        }
    }
}

internal static class Errors
{
    public static readonly Error CandidateProfileMissing = Error.NotFound("Tai khoan chua co ho so ung vien.");

    public static readonly Error ApplicationNotFound = Error.NotFound("Khong tim thay don ung tuyen.");

    public static readonly Error InterviewNotFound = Error.NotFound("Khong tim thay buoi phong van.");

    public static readonly Error NotJobOwner = Error.Forbidden("Ban khong so huu tin tuyen dung cua don nay.");
}
