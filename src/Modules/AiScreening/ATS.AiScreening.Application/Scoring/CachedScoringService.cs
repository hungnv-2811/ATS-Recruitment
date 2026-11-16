using System.Diagnostics;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace ATS.AiScreening.Application.Scoring;

/// <summary>Ket qua mot lan cham: diem + co trung cache khong + dong cache nao (cho ai_scores.cache_id).</summary>
public sealed record ScoringOutcome(ScreeningResult Result, bool CacheHit, Guid? CacheId, string ModelVersion);

/// <summary>
/// Buoc [3]–[5a] cua pipeline (ai-integration.md muc 1): tinh CacheKey → tra cache → goi
/// <see cref="IAiScoringService"/> → ghi cache. Dung chung cho preview (API) va sang loc lo (Worker).
/// </summary>
/// <remarks>
/// Tach "tra cache" va "cham + ghi cache" thanh hai buoc de preview chen viec TRU LUOT vao giua:
/// trung cache thi khong ton tien nen khong tru luot (RB2).
/// Ket qua Keyword KHONG ghi cache: mien phi, va lan sau con co co hoi goi lai OpenAI.
/// </remarks>
public sealed partial class CachedScoringService
{
    public const string KeywordAdapter = "Keyword";

    private readonly IAiScoringService _scoring;
    private readonly IAiScoreCacheRepository _cache;
    private readonly AiScreeningOptions _options;
    private readonly ILogger<CachedScoringService> _logger;

    public CachedScoringService(
        IAiScoringService scoring,
        IAiScoreCacheRepository cache,
        AiScreeningOptions options,
        ILogger<CachedScoringService> logger)
    {
        _scoring = scoring;
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    public CacheKey KeyFor(AnonymizedCv cv, JobRequirement jd)
    {
        ArgumentNullException.ThrowIfNull(cv);
        ArgumentNullException.ThrowIfNull(jd);
        return CacheKey.From(cv.Text, JobRequirementParser.ToCacheText(jd), _options.ModelVersion, _options.PromptVersion);
    }

    /// <summary>Tra cache; trung thi kho tang hit_count. Khong goi AI.</summary>
    public async Task<ScoringOutcome?> TryGetCachedAsync(CacheKey key, CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync(key, ct).ConfigureAwait(false);
        if (cached is null)
        {
            return null;
        }

        var cacheId = await _cache.FindIdAsync(key, ct).ConfigureAwait(false);
        LogScoring(_logger, true, cached.AdapterUsed, 0, cached.Score.Value, key.Value, cached.ModelVersion, cached.PromptVersion);

        var result = new ScreeningResult(cached.Score, cached.Summary, cached.Strengths, cached.Gaps, cached.AdapterUsed);
        return new ScoringOutcome(result, CacheHit: true, cacheId, cached.ModelVersion);
    }

    /// <summary>Goi pipeline AI (co fallback) roi ghi cache neu khong phai Keyword.</summary>
    public async Task<ScoringOutcome> ScoreAndCacheAsync(CacheKey key, AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default)
    {
        var watch = Stopwatch.StartNew();
        var result = await _scoring.ScoreAsync(cv, jd, ct).ConfigureAwait(false);
        watch.Stop();

        LogScoring(_logger, false, result.AdapterUsed, watch.ElapsedMilliseconds, result.Score.Value, key.Value, _options.ModelVersion, _options.PromptVersion);

        Guid? cacheId = null;
        if (!string.Equals(result.AdapterUsed, KeywordAdapter, StringComparison.Ordinal))
        {
            await _cache.SaveAsync(key, result, _options.ModelVersion, _options.PromptVersion, ct).ConfigureAwait(false);
            cacheId = await _cache.FindIdAsync(key, ct).ConfigureAwait(false);
        }

        return new ScoringOutcome(result, CacheHit: false, cacheId, _options.ModelVersion);
    }

    /// <summary>Tra cache truoc, truot thi cham — dung khi khong can tru luot giua chung (Worker).</summary>
    public async Task<ScoringOutcome> ScoreAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default)
    {
        var key = KeyFor(cv, jd);
        return await TryGetCachedAsync(key, ct).ConfigureAwait(false)
               ?? await ScoreAndCacheAsync(key, cv, jd, ct).ConfigureAwait(false);
    }

    // Log moi lan cham (ai-integration.md muc 7) — TUYET DOI khong log noi dung CV.
    [LoggerMessage(
        EventId = 4100,
        Level = LogLevel.Information,
        Message = "ai.scoring cache_hit={CacheHit} adapter={Adapter} duration_ms={DurationMs} score={Score} cache_key={CacheKey} model={Model} prompt_version={PromptVersion}")]
    private static partial void LogScoring(ILogger logger, bool cacheHit, string adapter, long durationMs, int score, string cacheKey, string model, string promptVersion);
}
