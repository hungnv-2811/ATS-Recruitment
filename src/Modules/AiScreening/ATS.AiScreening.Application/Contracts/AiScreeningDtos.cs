using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Entities;

namespace ATS.AiScreening.Application.Contracts;

// DTO cua bang "AI — TV4" trong docs/contracts.md muc 8.3. Doi ten/thu tu truong = bao TV3 truoc.

public sealed record ScorePreviewRequest(Guid CvId, Guid JobId);

public sealed record ScorePreviewResponse(
    int Score,
    string Summary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    string AdapterUsed,
    bool CacheHit,
    int PreviewRemainingToday);

public sealed record QuotaDto(
    int PreviewUsedToday,
    int PreviewLimitPerDay,
    int PreviewRemainingToday,
    int BatchUsedToday,
    int BatchLimitPerDay,
    DateTimeOffset ResetsAt)
{
    public static QuotaDto From(QuotaStatus status, DateTimeOffset resetsAt)
    {
        ArgumentNullException.ThrowIfNull(status);
        return new QuotaDto(
            status.PreviewUsedToday,
            status.PreviewLimitPerDay,
            status.PreviewRemainingToday,
            status.BatchUsedToday,
            status.BatchLimitPerDay,
            resetsAt);
    }
}

public sealed record StartScreeningRequest(Guid JobId);

/// <param name="Status">"Queued" | "Running" | "Done" | "Cancelled".</param>
public sealed record ScreeningJobDto(
    Guid Id,
    Guid JobId,
    int TargetCount,
    int DoneCount,
    int FailedCount,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt)
{
    public static ScreeningJobDto From(ScreeningJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        return new ScreeningJobDto(
            job.Id,
            job.JobId,
            job.TargetCount,
            job.DoneCount,
            job.FailedCount,
            job.Status.ToString(),
            job.StartedAt,
            job.FinishedAt);
    }
}

/// <param name="AdapterUsed">"OpenAI" | "Embedding" | "Keyword" | "Fake"; null khi chua co diem.</param>
/// <param name="ScreeningStatus">"Pending" | "Processing" | "Scored" | "Failed".</param>
public sealed record AiScoreDto(
    Guid ApplicationId,
    int? Score,
    string? Summary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    string? AdapterUsed,
    string ScreeningStatus,
    DateTimeOffset? ScoredAt)
{
    public static AiScoreDto From(AiScore score)
    {
        ArgumentNullException.ThrowIfNull(score);
        return new AiScoreDto(
            score.ApplicationId,
            score.Score,
            score.Summary,
            score.Strengths,
            score.Gaps,
            score.AdapterUsed,
            score.ScreeningStatus.ToString(),
            score.ScoredAt);
    }

    /// <summary>Don chua tung duoc xep hang doi — van hien, cot diem trong (RB9).</summary>
    public static AiScoreDto PendingFor(Guid applicationId)
        => new(applicationId, null, null, [], [], null, nameof(Domain.ScreeningStatus.Pending), null);
}

/// <param name="Category">"technical" | "behavioral" | "situational".</param>
/// <param name="Source">"AI" | "Manual" | "Template".</param>
public sealed record InterviewQuestionDto(string Category, string Question, string Source)
{
    public static InterviewQuestionDto From(InterviewQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);
        return new InterviewQuestionDto(question.Category, question.Question, question.Source);
    }
}

public sealed record SaveQuestionsRequest(IReadOnlyList<InterviewQuestionDto> Questions);
