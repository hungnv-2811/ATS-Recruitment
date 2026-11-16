using ATS.AiScreening.Application.Contracts;
using ATS.AiScreening.Application.Ports;
using ATS.AiScreening.Domain.Ports;
using ATS.SharedKernel;

namespace ATS.AiScreening.Application.Handlers;

/// <summary>Cac truy van doc cua module AI: diem cua mot don, tien do lo, han muc hom nay.</summary>
public sealed class AiScreeningQueries
{
    private readonly IRecruitmentReader _recruitment;
    private readonly IAiScoreRepository _scores;
    private readonly IScreeningJobRepository _jobs;
    private readonly IAiUsageQuota _quota;
    private readonly TimeProvider _clock;

    public AiScreeningQueries(
        IRecruitmentReader recruitment,
        IAiScoreRepository scores,
        IScreeningJobRepository jobs,
        IAiUsageQuota quota,
        TimeProvider clock)
    {
        _recruitment = recruitment;
        _scores = scores;
        _jobs = jobs;
        _quota = quota;
        _clock = clock;
    }

    /// <summary>Han muc reset luc 00:00 UTC ngay ke tiep (bo dem theo ngay UTC).</summary>
    public static DateTimeOffset NextResetUtc(DateTimeOffset now)
    {
        var utc = now.ToUniversalTime();
        return new DateTimeOffset(utc.Date.AddDays(1), TimeSpan.Zero);
    }

    public async Task<QuotaDto> GetQuotaAsync(Guid userId, CancellationToken ct = default)
    {
        var status = await _quota.GetStatusAsync(userId, ct).ConfigureAwait(false);
        return QuotaDto.From(status, NextResetUtc(_clock.GetUtcNow()));
    }

    /// <summary>HR so huu tin, hoac ung vien so huu don. Nguoi khac: 403.</summary>
    public async Task<Result<AiScoreDto>> GetAiScoreAsync(Guid userId, string? role, Guid applicationId, CancellationToken ct = default)
    {
        var application = await _recruitment.GetApplicationAsync(applicationId, ct).ConfigureAwait(false);
        if (application is null)
        {
            return Result.Failure<AiScoreDto>(Error.NotFound("Khong tim thay don ung tuyen."));
        }

        var allowed = (CallerRoles.Is(role, CallerRoles.Hr) && application.Job.HrOwnerUserId == userId)
                      || (CallerRoles.Is(role, CallerRoles.Candidate) && application.Cv.CandidateUserId == userId);
        if (!allowed)
        {
            return Result.Failure<AiScoreDto>(Error.Forbidden("Ban khong co quyen xem diem AI cua don nay."));
        }

        var score = await _scores.GetByApplicationAsync(applicationId, ct).ConfigureAwait(false);
        return score is null ? AiScoreDto.PendingFor(applicationId) : AiScoreDto.From(score);
    }

    /// <summary>Chi HR da khoi chay lo moi xem duoc; nguoi khac nhan 404 (khong lo id).</summary>
    public async Task<Result<ScreeningJobDto>> GetScreeningJobAsync(Guid hrUserId, Guid screeningJobId, CancellationToken ct = default)
    {
        var job = await _jobs.GetByIdAsync(screeningJobId, ct).ConfigureAwait(false);
        return job is null || job.StartedByHrId != hrUserId
            ? Result.Failure<ScreeningJobDto>(Error.NotFound("Khong tim thay lo sang loc."))
            : ScreeningJobDto.From(job);
    }

    public async Task<Result<ScreeningJobDto>> GetLatestScreeningJobAsync(Guid hrUserId, Guid jobId, CancellationToken ct = default)
    {
        var posting = await _recruitment.GetJobAsync(jobId, ct).ConfigureAwait(false);
        if (posting is null)
        {
            return Result.Failure<ScreeningJobDto>(Error.NotFound("Khong tim thay tin tuyen dung."));
        }

        if (posting.HrOwnerUserId != hrUserId)
        {
            return Result.Failure<ScreeningJobDto>(Error.Forbidden("Chi HR so huu tin moi xem duoc tien do sang loc."));
        }

        var latest = await _jobs.GetLatestByJobAsync(jobId, ct).ConfigureAwait(false);
        return latest is null
            ? Result.Failure<ScreeningJobDto>(Error.NotFound("Tin nay chua chay sang loc lan nao."))
            : ScreeningJobDto.From(latest);
    }
}
