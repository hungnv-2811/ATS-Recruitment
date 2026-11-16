namespace ATS.Persistence.Reporting;

/// <summary>Dashboard bao cao — docs/contracts.md muc 8.3 (TV1).</summary>
/// <param name="ApplicationsByStatus">Trang thai don (chuoi enum) → so don. Luon du 5 trang thai, thieu thi 0.</param>
/// <param name="HiredRate">So don <c>Hired</c> / tong so don, trong [0, 1]; 0 khi chua co don.</param>
/// <param name="TopJobs">Toi da 5 tin nhieu don nhat.</param>
public sealed record DashboardDto(
    int TotalJobs,
    int OpenJobs,
    int TotalApplications,
    IReadOnlyDictionary<string, int> ApplicationsByStatus,
    double HiredRate,
    IReadOnlyList<TopJobDto> TopJobs,
    AiDashboardDto Ai);

public sealed record TopJobDto(Guid JobId, string Title, int ApplicationCount);

/// <param name="ScoredCount">So dong ai_scores trang thai <c>Scored</c> (trong pham vi HR/he thong).</param>
/// <param name="FailedCount">So dong ai_scores trang thai <c>Failed</c>.</param>
/// <param name="CacheEntries">So dong ai_score_cache — moi dong la mot lan THAT SU goi model (cache miss).</param>
/// <param name="CacheHits">Tong hit_count — so lan dung lai ket qua khong ton tien.</param>
/// <param name="CacheHitRate">CacheHits / (CacheHits + CacheEntries), trong [0, 1].</param>
/// <param name="ByAdapter">Adapter (<c>OpenAI</c>/<c>Embedding</c>/<c>Keyword</c>/<c>Fake</c>) → so diem Scored.</param>
public sealed record AiDashboardDto(
    int ScoredCount,
    int FailedCount,
    int CacheEntries,
    long CacheHits,
    double CacheHitRate,
    IReadOnlyDictionary<string, int> ByAdapter);
