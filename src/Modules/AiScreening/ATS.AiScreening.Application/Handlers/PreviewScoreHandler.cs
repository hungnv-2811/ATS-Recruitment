using ATS.AiScreening.Application.Contracts;
using ATS.AiScreening.Application.Ports;
using ATS.AiScreening.Application.Scoring;
using ATS.AiScreening.Domain.Ports;
using ATS.SharedKernel;

namespace ATS.AiScreening.Application.Handlers;

/// <summary>
/// UC-05: ung vien xem % phu hop CV ↔ tin TRUOC khi nop. Chay DONG BO trong API (ngoai le ADR-2).
/// </summary>
/// <remarks>
/// Thu tu bat buoc: kiem quyen → an danh → TRA CACHE → (truot) TRU LUOT → cham.
/// Tra cache truoc tru luot: xem lai diem cu bao nhieu lan cung khong ton tien, nen khong tinh (RB2).
/// Tran thoi gian <c>Ai:PreviewTimeoutSeconds</c> nam trong AiScoringPipeline: qua tran thi tang
/// Keyword tra diem, ung vien khong bao gio nhan loi timeout.
/// </remarks>
public sealed class PreviewScoreHandler
{
    private readonly IRecruitmentReader _recruitment;
    private readonly IAnonymizer _anonymizer;
    private readonly CachedScoringService _scoring;
    private readonly IAiUsageQuota _quota;

    public PreviewScoreHandler(
        IRecruitmentReader recruitment,
        IAnonymizer anonymizer,
        CachedScoringService scoring,
        IAiUsageQuota quota)
    {
        _recruitment = recruitment;
        _anonymizer = anonymizer;
        _scoring = scoring;
        _quota = quota;
    }

    public async Task<Result<ScorePreviewResponse>> HandleAsync(
        Guid candidateUserId,
        ScorePreviewRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cv = await _recruitment.GetCvAsync(request.CvId, ct).ConfigureAwait(false);
        if (cv is null || cv.CandidateUserId != candidateUserId)
        {
            // CV cua nguoi khac tra 404 nhu khong ton tai: khong lo ra id nao co that.
            return Result.Failure<ScorePreviewResponse>(Error.NotFound("Khong tim thay CV."));
        }

        var job = await _recruitment.GetJobAsync(request.JobId, ct).ConfigureAwait(false);
        if (job is null || !job.IsOpen)
        {
            return Result.Failure<ScorePreviewResponse>(Error.NotFound("Tin tuyen dung khong ton tai hoac da dong."));
        }

        if (string.IsNullOrWhiteSpace(cv.ExtractedText))
        {
            return Result.Failure<ScorePreviewResponse>(Error.Validation(
                "CV nay khong trich duoc chu (file scan/anh?). Hay tai len ban PDF/DOCX co chu de xem diem."));
        }

        var anonymized = await _anonymizer.AnonymizeAsync(cv.ExtractedText, ct).ConfigureAwait(false);
        var jd = JobRequirementParser.Parse(job);
        var key = _scoring.KeyFor(anonymized, jd);

        var outcome = await _scoring.TryGetCachedAsync(key, ct).ConfigureAwait(false);
        if (outcome is null)
        {
            if (!await _quota.TryConsumePreviewAsync(candidateUserId, ct).ConfigureAwait(false))
            {
                return Result.Failure<ScorePreviewResponse>(Error.QuotaExceeded(
                    "Ban da dung het luot xem diem AI hom nay. Luot moi duoc cap lai luc 00:00 UTC."));
            }

            outcome = await _scoring.ScoreAndCacheAsync(key, anonymized, jd, ct).ConfigureAwait(false);
        }

        var status = await _quota.GetStatusAsync(candidateUserId, ct).ConfigureAwait(false);
        var result = outcome.Result;

        return new ScorePreviewResponse(
            result.Score.Value,
            result.Summary,
            result.Strengths,
            result.Gaps,
            result.AdapterUsed,
            outcome.CacheHit,
            status.PreviewRemainingToday);
    }
}
