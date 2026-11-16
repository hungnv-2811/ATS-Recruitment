using ATS.AiScreening.Application.Abstractions;
using ATS.AiScreening.Application.Ports;
using ATS.AiScreening.Application.Scoring;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Entities;
using ATS.AiScreening.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace ATS.AiScreening.Application.Handlers;

/// <summary>
/// Worker goi cho MOI <see cref="ScreeningTask"/> lay tu hang doi: doc CV+JD → an danh → cham
/// (co cache, co fallback) → ghi <c>ai_scores</c> → cong tien do lo.
/// </summary>
/// <remarks>
/// Khong bao gio nem loi ra ngoai vi ly do nghiep vu: mot CV hong khong duoc lam chet ca lo.
/// Loi → AiScore <c>Failed</c> + <c>FailedCount++</c>. HR van chuyen trang thai don duoc (RB9).
/// <see cref="ScreeningTask.ScreeningJobId"/> rong = cham le ngay sau khi nop don, khong co lo nao de cong.
/// </remarks>
public sealed partial class ProcessScreeningHandler : IProcessScreeningHandler
{
    private readonly IRecruitmentReader _recruitment;
    private readonly IAnonymizer _anonymizer;
    private readonly CachedScoringService _scoring;
    private readonly IAiScoreRepository _scores;
    private readonly IScreeningJobRepository _jobs;
    private readonly TimeProvider _clock;
    private readonly ILogger<ProcessScreeningHandler> _logger;

    public ProcessScreeningHandler(
        IRecruitmentReader recruitment,
        IAnonymizer anonymizer,
        CachedScoringService scoring,
        IAiScoreRepository scores,
        IScreeningJobRepository jobs,
        TimeProvider clock,
        ILogger<ProcessScreeningHandler> logger)
    {
        _recruitment = recruitment;
        _anonymizer = anonymizer;
        _scoring = scoring;
        _scores = scores;
        _jobs = jobs;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleAsync(ScreeningTask task, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        var hasBatch = task.ScreeningJobId != Guid.Empty;
        if (hasBatch)
        {
            var batch = await _jobs.GetByIdAsync(task.ScreeningJobId, ct).ConfigureAwait(false);
            if (batch is { Status: ScreeningJobStatus.Cancelled })
            {
                LogSkippedCancelled(_logger, task.ApplicationId, task.ScreeningJobId);
                return;
            }
        }

        var score = await _scores.GetByApplicationAsync(task.ApplicationId, ct).ConfigureAwait(false)
                    ?? AiScore.CreatePending(task.ApplicationId, _clock.GetUtcNow());
        score.MarkProcessing(_clock.GetUtcNow());
        await _scores.UpsertAsync(score, ct).ConfigureAwait(false);

        string? failure;
        try
        {
            failure = await ScoreAsync(task.ApplicationId, score, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // Ke ca Keyword cung loi (khong le xay ra) — ghi Failed, KHONG nem: lo phai chay tiep.
            LogScoringFailed(_logger, ex, task.ApplicationId);
            failure = $"Loi khi cham: {ex.GetType().Name}";
        }

        if (failure is not null)
        {
            score.MarkFailed(failure, _clock.GetUtcNow());
        }

        await _scores.UpsertAsync(score, ct).ConfigureAwait(false);

        if (hasBatch)
        {
            await _jobs.UpdateProgressAsync(
                task.ScreeningJobId,
                done: failure is null ? 1 : 0,
                failed: failure is null ? 0 : 1,
                ct).ConfigureAwait(false);
        }
    }

    /// <returns>null neu cham xong; nguoc lai ly do that bai (khong chua noi dung CV).</returns>
    private async Task<string?> ScoreAsync(Guid applicationId, AiScore score, CancellationToken ct)
    {
        var application = await _recruitment.GetApplicationAsync(applicationId, ct).ConfigureAwait(false);
        if (application is null)
        {
            return "Khong tim thay don ung tuyen.";
        }

        if (string.IsNullOrWhiteSpace(application.Cv.ExtractedText))
        {
            return "CV khong trich duoc chu (file scan/anh) — HR doc CV truc tiep.";
        }

        var anonymized = await _anonymizer.AnonymizeAsync(application.Cv.ExtractedText, ct).ConfigureAwait(false);
        var jd = JobRequirementParser.Parse(application.Job);
        var outcome = await _scoring.ScoreAsync(anonymized, jd, ct).ConfigureAwait(false);

        score.MarkScored(outcome.Result, outcome.ModelVersion, outcome.CacheId, _clock.GetUtcNow());
        return null;
    }

    [LoggerMessage(EventId = 4110, Level = LogLevel.Warning, Message = "Cham diem don {ApplicationId} that bai")]
    private static partial void LogScoringFailed(ILogger logger, Exception ex, Guid applicationId);

    [LoggerMessage(EventId = 4111, Level = LogLevel.Information, Message = "Bo qua don {ApplicationId}: lo {ScreeningJobId} da bi huy")]
    private static partial void LogSkippedCancelled(ILogger logger, Guid applicationId, Guid screeningJobId);
}
