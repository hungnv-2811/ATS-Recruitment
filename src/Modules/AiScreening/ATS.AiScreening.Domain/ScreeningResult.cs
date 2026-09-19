namespace ATS.AiScreening.Domain;

/// <summary>Ket qua cham diem. <paramref name="AdapterUsed"/> la mot phan cua ket qua, khong phai cot debug.</summary>
public sealed record ScreeningResult(
    Score Score,
    string Summary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    string AdapterUsed);

/// <summary>Mot dong trong kho cache, doc ra tu <c>ai_score_cache</c>.</summary>
public sealed record CachedScore(
    Score Score,
    string Summary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    string AdapterUsed,
    string ModelVersion,
    string PromptVersion,
    DateTimeOffset CreatedAt);

/// <summary>Yeu cau cua mot tin tuyen dung, dua vao prompt.</summary>
public sealed record JobRequirement(
    string Title,
    string Description,
    IReadOnlyList<string> RequiredSkills,
    IReadOnlyList<string> NiceToHaveSkills,
    int MinYearsExperience);

public sealed record InterviewQuestion(
    string Category,
    string Question,
    string Source);

/// <summary>Mot viec trong hang doi Redis.</summary>
public sealed record ScreeningTask(
    Guid ApplicationId,
    Guid ScreeningJobId);

/// <summary>Tinh trang han muc AI trong ngay — hien thuc RB2.</summary>
public sealed record QuotaStatus(
    int PreviewUsedToday,
    int PreviewLimitPerDay,
    int BatchUsedToday,
    int BatchLimitPerDay)
{
    public int PreviewRemainingToday => Math.Max(0, PreviewLimitPerDay - PreviewUsedToday);
}
