using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace ATS.Persistence.Reporting;

/// <summary>
/// Dashboard bang SQL tho qua ket noi cua <see cref="AtsDbContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// Vi sao SQL tho: ATS.Persistence KHONG tham chieu entity cua module nao (muc 3.3 architecture.md),
/// nen khong co DbSet de LINQ. Ten bang/cot lay dung docs/database-design.md + quy uoc
/// snake_case; enum luu dang CHUOI (<c>'Open'</c>, <c>'Hired'</c>, <c>'Scored'</c>...).
/// Doi ten property entity ma khong sua file nay => dashboard hong luc chay — pha tich hop phai
/// chay thu tren Postgres that (xem handover TV1).
/// </para>
/// <para>
/// Mot lenh, nam cau SELECT, mot round-trip. Pham vi HR: chi tin co <c>jobs.hr_owner_id = @user_id</c>;
/// Admin: <c>@all = true</c>. Cache AI (<c>ai_score_cache</c>) khong gan voi tin nao nen LUON tinh
/// toan he thong.
/// </para>
/// </remarks>
public sealed class DashboardReportService : IDashboardReportService
{
    /// <summary>Toan bo SQL cua dashboard (public de test doi chieu ten bang).</summary>
    public const string SqlText = """
        -- 1. Tin tuyen dung
        SELECT COUNT(*)::int                                    AS total_jobs,
               COUNT(*) FILTER (WHERE j.status = 'Open')::int    AS open_jobs
        FROM   recruitment.jobs j
        WHERE  (@all OR j.hr_owner_id = @user_id);

        -- 2. Don ung tuyen theo trang thai
        SELECT a.status, COUNT(*)::int AS n
        FROM   recruitment.applications a
        JOIN   recruitment.jobs j ON j.id = a.job_id
        WHERE  (@all OR j.hr_owner_id = @user_id)
        GROUP  BY a.status;

        -- 3. Top 5 tin nhieu don nhat
        SELECT j.id, j.title, COUNT(a.id)::int AS n
        FROM   recruitment.jobs j
        LEFT   JOIN recruitment.applications a ON a.job_id = j.id
        WHERE  (@all OR j.hr_owner_id = @user_id)
        GROUP  BY j.id, j.title
        ORDER  BY n DESC, j.title
        LIMIT  5;

        -- 4. Diem AI theo trang thai + adapter
        SELECT s.screening_status, s.adapter_used, COUNT(*)::int AS n
        FROM   aiscreening.ai_scores s
        JOIN   recruitment.applications a ON a.id = s.application_id
        JOIN   recruitment.jobs j         ON j.id = a.job_id
        WHERE  (@all OR j.hr_owner_id = @user_id)
        GROUP  BY s.screening_status, s.adapter_used;

        -- 5. Cache AI (toan he thong)
        SELECT COUNT(*)::int AS entries, COALESCE(SUM(c.hit_count), 0)::bigint AS hits
        FROM   aiscreening.ai_score_cache c;
        """;

    private readonly AtsDbContext _db;

    public DashboardReportService(AtsDbContext db) => _db = db;

    public async Task<DashboardDto> GetDashboardAsync(Guid? hrOwnerUserId, CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();
        await _db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
        try
        {
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = SqlText;
                AddParameter(command, "all", DbType.Boolean, hrOwnerUserId is null);
                AddParameter(command, "user_id", DbType.Guid, hrOwnerUserId ?? Guid.Empty);

                var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    // 1
                    var totalJobs = 0;
                    var openJobs = 0;
                    if (await reader.ReadAsync(ct).ConfigureAwait(false))
                    {
                        totalJobs = reader.GetInt32(0);
                        openJobs = reader.GetInt32(1);
                    }

                    // 2
                    await reader.NextResultAsync(ct).ConfigureAwait(false);
                    var byStatus = new List<(string Status, int Count)>();
                    while (await reader.ReadAsync(ct).ConfigureAwait(false))
                    {
                        byStatus.Add((reader.GetString(0), reader.GetInt32(1)));
                    }

                    // 3
                    await reader.NextResultAsync(ct).ConfigureAwait(false);
                    var topJobs = new List<TopJobDto>();
                    while (await reader.ReadAsync(ct).ConfigureAwait(false))
                    {
                        topJobs.Add(new TopJobDto(reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2)));
                    }

                    // 4
                    await reader.NextResultAsync(ct).ConfigureAwait(false);
                    var aiRows = new List<(string Status, string? Adapter, int Count)>();
                    while (await reader.ReadAsync(ct).ConfigureAwait(false))
                    {
                        aiRows.Add((
                            reader.GetString(0),
                            await reader.IsDBNullAsync(1, ct).ConfigureAwait(false) ? null : reader.GetString(1),
                            reader.GetInt32(2)));
                    }

                    // 5
                    await reader.NextResultAsync(ct).ConfigureAwait(false);
                    var cacheEntries = 0;
                    var cacheHits = 0L;
                    if (await reader.ReadAsync(ct).ConfigureAwait(false))
                    {
                        cacheEntries = reader.GetInt32(0);
                        cacheHits = reader.GetInt64(1);
                    }

                    return DashboardCalculator.Build(
                        totalJobs, openJobs, byStatus, topJobs, aiRows, cacheEntries, cacheHits);
                }
            }
        }
        finally
        {
            await _db.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    private static void AddParameter(DbCommand command, string name, DbType type, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}

/// <summary>Phan TINH TOAN cua dashboard, tach khoi SQL de kiem bang unit test.</summary>
public static class DashboardCalculator
{
    /// <summary>Thu tu hien thi = thu tu enum ApplicationStatus (Recruitment.Domain).</summary>
    public static readonly IReadOnlyList<string> ApplicationStatuses =
        ["Applied", "Screening", "Interview", "Hired", "Rejected"];

    public const string HiredStatus = "Hired";
    public const string ScoredStatus = "Scored";
    public const string FailedStatus = "Failed";

    public static DashboardDto Build(
        int totalJobs,
        int openJobs,
        IEnumerable<(string Status, int Count)> applicationsByStatus,
        IReadOnlyList<TopJobDto> topJobs,
        IEnumerable<(string Status, string? Adapter, int Count)> aiScores,
        int cacheEntries,
        long cacheHits)
    {
        ArgumentNullException.ThrowIfNull(applicationsByStatus);
        ArgumentNullException.ThrowIfNull(topJobs);
        ArgumentNullException.ThrowIfNull(aiScores);

        // Du 5 trang thai (UI ve bieu do khong phai doan trang thai nao vang mat).
        var byStatus = ApplicationStatuses.ToDictionary(s => s, _ => 0, StringComparer.Ordinal);
        foreach (var (status, count) in applicationsByStatus)
        {
            byStatus[status] = byStatus.GetValueOrDefault(status) + count;
        }

        var totalApplications = byStatus.Values.Sum();
        var hiredRate = Ratio(byStatus.GetValueOrDefault(HiredStatus), totalApplications);

        var scored = 0;
        var failed = 0;
        var byAdapter = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (status, adapter, count) in aiScores)
        {
            if (string.Equals(status, ScoredStatus, StringComparison.Ordinal))
            {
                scored += count;
                if (!string.IsNullOrEmpty(adapter))
                {
                    byAdapter[adapter] = byAdapter.GetValueOrDefault(adapter) + count;
                }
            }
            else if (string.Equals(status, FailedStatus, StringComparison.Ordinal))
            {
                failed += count;
            }
        }

        // Moi dong cache = 1 lan goi model that (miss); hit_count = so lan dung lai (hit).
        var cacheHitRate = Ratio(cacheHits, cacheHits + cacheEntries);

        return new DashboardDto(
            totalJobs,
            openJobs,
            totalApplications,
            byStatus,
            hiredRate,
            topJobs,
            new AiDashboardDto(scored, failed, cacheEntries, cacheHits, cacheHitRate, byAdapter));
    }

    private static double Ratio(long part, long whole)
        => whole <= 0 ? 0d : Math.Round((double)part / whole, 4);
}
