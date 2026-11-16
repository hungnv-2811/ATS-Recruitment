using ATS.AiScreening.Application.Contracts;
using ATS.AiScreening.Application.Ports;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Entities;
using ATS.AiScreening.Domain.Ports;
using ATS.SharedKernel;

namespace ATS.AiScreening.Application.Handlers;

/// <summary>
/// HR bam "Sang loc AI" cho mot tin: tru luot lo, tao ScreeningJob, day MOI don cua tin vao hang doi.
/// API tra 202 ngay; Worker cham dan, UI polling <c>GET /api/screening-jobs/{id}</c> (ADR-2).
/// </summary>
public sealed class StartScreeningHandler
{
    private readonly IRecruitmentReader _recruitment;
    private readonly IAiUsageQuota _quota;
    private readonly IScreeningJobRepository _jobs;
    private readonly IAiScoreRepository _scores;
    private readonly IScreeningQueue _queue;
    private readonly AiScreeningOptions _options;
    private readonly TimeProvider _clock;

    public StartScreeningHandler(
        IRecruitmentReader recruitment,
        IAiUsageQuota quota,
        IScreeningJobRepository jobs,
        IAiScoreRepository scores,
        IScreeningQueue queue,
        AiScreeningOptions options,
        TimeProvider clock)
    {
        _recruitment = recruitment;
        _quota = quota;
        _jobs = jobs;
        _scores = scores;
        _queue = queue;
        _options = options;
        _clock = clock;
    }

    public async Task<Result<ScreeningJobDto>> HandleAsync(Guid hrUserId, StartScreeningRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var job = await _recruitment.GetJobAsync(request.JobId, ct).ConfigureAwait(false);
        if (job is null)
        {
            return Result.Failure<ScreeningJobDto>(Error.NotFound("Khong tim thay tin tuyen dung."));
        }

        if (job.HrOwnerUserId != hrUserId)
        {
            return Result.Failure<ScreeningJobDto>(Error.Forbidden("Chi HR so huu tin moi duoc sang loc tin nay."));
        }

        var applicationIds = await _recruitment.ListApplicationIdsByJobAsync(job.JobId, ct).ConfigureAwait(false);
        if (applicationIds.Count > _options.MaxApplicationsPerBatch)
        {
            return Result.Failure<ScreeningJobDto>(Error.Validation(
                $"Mot lo toi da {_options.MaxApplicationsPerBatch} ho so; tin nay co {applicationIds.Count}."));
        }

        if (!await _quota.TryConsumeBatchAsync(hrUserId, ct).ConfigureAwait(false))
        {
            return Result.Failure<ScreeningJobDto>(Error.QuotaExceeded(
                $"Da chay du {_options.BatchLimitPerDay} lo sang loc hom nay. Luot moi duoc cap lai luc 00:00 UTC."));
        }

        var now = _clock.GetUtcNow();
        var screeningJob = ScreeningJob.Start(job.JobId, hrUserId, applicationIds.Count, now);
        await _jobs.AddAsync(screeningJob, ct).ConfigureAwait(false);

        foreach (var applicationId in applicationIds)
        {
            // Danh dau Pending TRUOC khi enqueue: UI thay ngay "dang cho cham" thay vi diem cu.
            var score = await _scores.GetByApplicationAsync(applicationId, ct).ConfigureAwait(false)
                        ?? AiScore.CreatePending(applicationId, now);
            score.MarkPending(now);
            await _scores.UpsertAsync(score, ct).ConfigureAwait(false);

            await _queue.EnqueueAsync(new ScreeningTask(applicationId, screeningJob.Id), ct).ConfigureAwait(false);
        }

        return ScreeningJobDto.From(screeningJob);
    }
}
