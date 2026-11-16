using ATS.SharedKernel;

namespace ATS.AiScreening.Domain.Entities;

/// <summary>
/// Mot dong KHO TRA CUU theo noi dung (bang <c>aiscreening.ai_score_cache</c>).
/// Khong co application_id — preview cua ung vien luc chua nop don van cache duoc (UC-05).
/// </summary>
public sealed class AiScoreCacheEntry : Entity<Guid>
{
    private AiScoreCacheEntry(
        Guid id,
        string cacheKey,
        ScreeningResult result,
        string modelVersion,
        string promptVersion,
        DateTimeOffset now)
        : base(id)
    {
        CacheKey = cacheKey;
        Score = result.Score.Value;
        Summary = result.Summary;
        Strengths = result.Strengths.ToArray();
        Gaps = result.Gaps.ToArray();
        AdapterUsed = result.AdapterUsed;
        ModelVersion = modelVersion;
        PromptVersion = promptVersion;
        CreatedAt = now;
    }

    /// <summary>Danh cho EF Core.</summary>
    private AiScoreCacheEntry()
    {
        CacheKey = string.Empty;
        Summary = string.Empty;
        AdapterUsed = string.Empty;
        ModelVersion = string.Empty;
        PromptVersion = string.Empty;
    }

    /// <summary>SHA256(cv|jd|model|prompt), hex thuong — unique.</summary>
    public string CacheKey { get; private set; }

    public int Score { get; private set; }

    public string Summary { get; private set; }

    public IReadOnlyList<string> Strengths { get; private set; } = [];

    public IReadOnlyList<string> Gaps { get; private set; } = [];

    public string AdapterUsed { get; private set; }

    public string ModelVersion { get; private set; }

    public string PromptVersion { get; private set; }

    /// <summary>So lan trung cache — do ti le cache hit cho RB2.</summary>
    public int HitCount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? LastHitAt { get; private set; }

    public static AiScoreCacheEntry Create(
        CacheKey key,
        ScreeningResult result,
        string modelVersion,
        string promptVersion,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(promptVersion);

        return new AiScoreCacheEntry(Guid.CreateVersion7(), key.Value, result, modelVersion, promptVersion, now);
    }

    public void RegisterHit(DateTimeOffset now)
    {
        HitCount++;
        LastHitAt = now;
    }

    public CachedScore ToCachedScore()
        => new(
            new Score(Score),
            Summary,
            Strengths,
            Gaps,
            AdapterUsed,
            ModelVersion,
            PromptVersion,
            CreatedAt);
}
