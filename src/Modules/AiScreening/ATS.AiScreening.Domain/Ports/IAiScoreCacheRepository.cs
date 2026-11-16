namespace ATS.AiScreening.Domain.Ports;

/// <summary>
/// Kho tra cuu theo NOI DUNG (bang <c>ai_score_cache</c>).
/// KHONG co application_id — nho vay preview cua ung vien luc chua nop don van cache duoc.
/// Xem docs/database-design.md muc 2.
/// </summary>
public interface IAiScoreCacheRepository
{
    /// <summary>Tra dong cache neu co; trung cache thi tang <c>hit_count</c> va <c>last_hit_at</c>.</summary>
    Task<CachedScore?> GetAsync(CacheKey key, CancellationToken ct = default);

    /// <summary>Ghi dong moi. Da co dong cung khoa (hai request dua nhau) thi bo qua, khong loi.</summary>
    Task SaveAsync(CacheKey key, ScreeningResult result, string modelVersion, string promptVersion, CancellationToken ct = default);

    /// <summary>Id cua dong cache — de <c>ai_scores.cache_id</c> tro ve. Khong tinh la mot lan trung cache.</summary>
    Task<Guid?> FindIdAsync(CacheKey key, CancellationToken ct = default);
}
