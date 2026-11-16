using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace ATS.AiScreening.Infrastructure.Scoring;

/// <summary>
/// Fallback 3 tang (ai-integration.md muc 5): OpenAI → Embedding → Keyword. Tang truoc loi thi
/// roi xuong tang sau; tang cuoi khong mang, khong tien — "cuoi cung luon co diem".
/// </summary>
/// <remarks>
/// Cac tang AI dung chung MOT ngan sach thoi gian (<c>Ai:PreviewTimeoutSeconds</c>, mac dinh 30s).
/// Het ngan sach thi bo qua moi tang AI con lai, chay thang tang cuoi: preview cua ung vien
/// (dong bo trong API) khong bao gio treo qua tran. <c>WaitAsync</c> ep tran ca khi adapter
/// lo khong ton trong CancellationToken.
/// Huy tu phia nguoi goi (ct) thi KHONG roi xuong — nem OperationCanceledException nhu binh thuong.
/// </remarks>
public sealed partial class AiScoringPipeline : IAiScoringService
{
    private readonly IReadOnlyList<IAiScoringService> _aiTiers;
    private readonly IAiScoringService _finalFallback;
    private readonly TimeSpan _aiBudget;
    private readonly ILogger<AiScoringPipeline> _logger;

    public AiScoringPipeline(
        IReadOnlyList<IAiScoringService> aiTiers,
        IAiScoringService finalFallback,
        TimeSpan aiBudget,
        ILogger<AiScoringPipeline> logger)
    {
        ArgumentNullException.ThrowIfNull(aiTiers);
        ArgumentNullException.ThrowIfNull(finalFallback);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(aiBudget, TimeSpan.Zero);

        _aiTiers = aiTiers;
        _finalFallback = finalFallback;
        _aiBudget = aiBudget;
        _logger = logger;
    }

    public async Task<ScreeningResult> ScoreAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default)
    {
        if (_aiTiers.Count > 0)
        {
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
            budget.CancelAfter(_aiBudget);

            foreach (var tier in _aiTiers)
            {
                try
                {
                    return await tier.ScoreAsync(cv, jd, budget.Token).WaitAsync(budget.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested && budget.IsCancellationRequested)
                {
                    LogBudgetExceeded(_logger, tier.GetType().Name, _aiBudget.TotalSeconds);
                    break;
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    LogTierFailed(_logger, ex, tier.GetType().Name);
                }
            }
        }

        return await _finalFallback.ScoreAsync(cv, jd, ct).ConfigureAwait(false);
    }

    [LoggerMessage(EventId = 4120, Level = LogLevel.Warning, Message = "Tang cham {Tier} loi — roi xuong tang sau")]
    private static partial void LogTierFailed(ILogger logger, Exception ex, string tier);

    [LoggerMessage(EventId = 4121, Level = LogLevel.Warning, Message = "Tang cham {Tier} vuot tran {Seconds}s — chuyen thang sang tang cuoi (Keyword)")]
    private static partial void LogBudgetExceeded(ILogger logger, string tier, double seconds);
}

/// <summary>Sinh cau hoi: OpenAI (neu co) → Template. Cung nguyen tac "luon co ket qua" nhu cham diem.</summary>
public sealed partial class InterviewQuestionPipeline : IInterviewQuestionGenerator
{
    private readonly IInterviewQuestionGenerator? _primary;
    private readonly IInterviewQuestionGenerator _fallback;
    private readonly TimeSpan _budget;
    private readonly ILogger<InterviewQuestionPipeline> _logger;

    public InterviewQuestionPipeline(
        IInterviewQuestionGenerator? primary,
        IInterviewQuestionGenerator fallback,
        TimeSpan budget,
        ILogger<InterviewQuestionPipeline> logger)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        _primary = primary;
        _fallback = fallback;
        _budget = budget;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InterviewQuestion>> GenerateAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default)
    {
        if (_primary is not null)
        {
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
            budget.CancelAfter(_budget);
            try
            {
                return await _primary.GenerateAsync(cv, jd, budget.Token).WaitAsync(budget.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                LogPrimaryFailed(_logger, ex, _primary.GetType().Name);
            }
        }

        return await _fallback.GenerateAsync(cv, jd, ct).ConfigureAwait(false);
    }

    [LoggerMessage(EventId = 4130, Level = LogLevel.Warning, Message = "Sinh cau hoi bang {Generator} loi — dung bo cau hoi mau")]
    private static partial void LogPrimaryFailed(ILogger logger, Exception ex, string generator);
}
