namespace ATS.AiScreening.Domain.Ports;

/// <summary>
/// Kho tra cuu theo NOI DUNG (bang <c>ai_score_cache</c>).
/// KHONG co application_id — nho vay preview cua ung vien luc chua nop don van cache duoc.
/// Xem docs/database-design.md muc 2.
/// </summary>
public interface IAiScoreCacheRepository
{
    Task<CachedScore?> GetAsync(CacheKey key, CancellationToken ct = default);

    Task SaveAsync(CacheKey key, ScreeningResult result, string modelVersion, string promptVersion, CancellationToken ct = default);
}
