using ATS.AiScreening.Domain.Ports;
using ATS.Persistence;
using ATS.Persistence.Reporting;
using ATS.Recruitment.Application.Contracts;
using ATS.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using JobApplication = ATS.Recruitment.Domain.Application;

namespace ATS.IntegrationTests.Postgres;

/// <summary>
/// Kiem nhung thu CHI database that moi lo ra: migration, seeder, SQL bao cao, quota nguyen tu,
/// rang buoc UNIQUE khi hai request dong thoi. Xem <see cref="PostgresFixture"/> de bat.
/// </summary>
[Trait("Category", "Postgres")]
public sealed class PostgresDatabaseTests : IClassFixture<PostgresFixture>
{
    private static readonly string[] SeededTables =
    [
        "identity.users", "recruitment.candidates", "recruitment.hr_profiles", "recruitment.jobs",
        "recruitment.cvs", "recruitment.applications", "recruitment.interviews", "recruitment.evaluations",
        "aiscreening.ai_scores", "aiscreening.ai_score_cache", "aiscreening.screening_jobs",
        "aiscreening.interview_questions",
    ];

    private readonly PostgresFixture _pg;

    public PostgresDatabaseTests(PostgresFixture pg) => _pg = pg;

    [Fact]
    public async Task Migration_ap_du_va_model_khong_con_thay_doi_chua_co_migration()
    {
        if (!_pg.IsAvailable)
        {
            return;
        }

        await using var scope = _pg.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());

        // Sua entity/config ma quen `dotnet ef migrations add` → test nay do (build van xanh).
        Assert.False(db.Database.HasPendingModelChanges(), "Model EF lech voi migration cuoi — chay dotnet ef migrations add.");

        var tables = await _pg.ScalarAsync(
            "SELECT count(*) FROM information_schema.tables WHERE table_schema IN ('identity','recruitment','aiscreening')");
        Assert.Equal(14, tables);
    }

    [Fact]
    public async Task Seeder_idempotent_chay_lan_hai_khong_them_dong_nao()
    {
        if (!_pg.IsAvailable)
        {
            return;
        }

        Assert.True(_pg.FirstSeedInserted);
        var before = await CountAllAsync();

        await using var scope = _pg.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
        var insertedAgain = await DatabaseSeeder.SeedAsync(db, cvFileRoot: null);

        Assert.False(insertedAgain);
        Assert.Equal(before, await CountAllAsync());
        Assert.True(before["recruitment.applications"] >= 30);
    }

    [Fact]
    public async Task Bao_cao_dashboard_chay_SQL_that_tren_du_lieu_seed()
    {
        if (!_pg.IsAvailable)
        {
            return;
        }

        await using var scope = _pg.Services.CreateAsyncScope();
        var reports = scope.ServiceProvider.GetRequiredService<IDashboardReportService>();

        var all = await reports.GetDashboardAsync(hrOwnerUserId: null);

        Assert.Equal(await _pg.ScalarAsync("SELECT count(*) FROM recruitment.jobs"), all.TotalJobs);
        Assert.Equal(await _pg.ScalarAsync("SELECT count(*) FROM recruitment.applications"), all.TotalApplications);
        Assert.Equal(all.TotalApplications, all.ApplicationsByStatus.Values.Sum());
        Assert.NotEmpty(all.TopJobs);

        var hrId = (Guid)(await _pg.RowsAsync("SELECT hr_owner_id FROM recruitment.jobs LIMIT 1"))[0][0];
        var mine = await reports.GetDashboardAsync(hrId);
        Assert.InRange(mine.TotalJobs, 1, all.TotalJobs - 1);
    }

    [Fact]
    public async Task Quota_preview_nguyen_tu_khi_goi_song_song_khong_vuot_tran()
    {
        if (!_pg.IsAvailable)
        {
            return;
        }

        var userId = (Guid)(await _pg.RowsAsync("SELECT id FROM identity.users WHERE role = 'Candidate' ORDER BY email DESC LIMIT 1"))[0][0];

        // 30 request dong thoi, moi request mot scope/DbContext rieng — nhu 30 lan bam cung luc.
        var attempts = Enumerable.Range(0, 30).Select(async _ =>
        {
            await using var scope = _pg.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IAiUsageQuota>().TryConsumePreviewAsync(userId);
        });
        var results = await Task.WhenAll(attempts);

        Assert.Equal(20, results.Count(ok => ok));
        await using var check = _pg.Services.CreateAsyncScope();
        var status = await check.ServiceProvider.GetRequiredService<IAiUsageQuota>().GetStatusAsync(userId);
        Assert.Equal(0, status.PreviewRemainingToday);
    }

    [Fact]
    public async Task Rang_buoc_unique_bi_vi_pham_duoc_dich_thanh_DuplicateKeyException()
    {
        if (!_pg.IsAvailable)
        {
            return;
        }

        var row = (await _pg.RowsAsync("SELECT job_id, candidate_id, cv_id FROM recruitment.applications LIMIT 1"))[0];

        await using var scope = _pg.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
        db.Add(JobApplication.Submit((Guid)row[0], (Guid)row[1], (Guid)row[2], DateTimeOffset.UtcNow));

        var ex = await Assert.ThrowsAsync<DuplicateKeyException>(() => db.SaveChangesAsync());
        Assert.Contains("job_id", ex.ConstraintName ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Nop_don_trung_dong_thoi_chi_tao_mot_don_con_lai_409()
    {
        if (!_pg.IsAvailable)
        {
            return;
        }

        // Mot ung vien + mot tin Open ma ung vien CHUA nop + mot CV cua ung vien do.
        var pick = await _pg.RowsAsync("""
            SELECT c.user_id, j.id, cv.id
            FROM recruitment.candidates c
            JOIN recruitment.cvs cv ON cv.candidate_id = c.id
            CROSS JOIN recruitment.jobs j
            WHERE j.status = 'Open'
              AND NOT EXISTS (SELECT 1 FROM recruitment.applications a WHERE a.job_id = j.id AND a.candidate_id = c.id)
            LIMIT 1
            """);
        Assert.NotEmpty(pick);
        var (userId, jobId, cvId) = ((Guid)pick[0][0], (Guid)pick[0][1], (Guid)pick[0][2]);

        var attempts = Enumerable.Range(0, 6).Select(async _ =>
        {
            await using var scope = _pg.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IApplicationService>()
                .ApplyAsync(userId, new ApplyRequest(jobId, cvId));
        });
        var results = await Task.WhenAll(attempts);

        Assert.Single(results, r => r.IsSuccess);
        Assert.All(results.Where(r => r.IsFailure), r => Assert.Equal("conflict", r.Error.Code));
        Assert.Equal(1, await _pg.ScalarAsync(
            $"SELECT count(*) FROM recruitment.applications WHERE job_id = '{jobId}' AND candidate_id = (SELECT id FROM recruitment.candidates WHERE user_id = '{userId}')"));
    }

    private async Task<Dictionary<string, long>> CountAllAsync()
    {
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var table in SeededTables)
        {
            counts[table] = await _pg.ScalarAsync($"SELECT count(*) FROM {table}");
        }

        return counts;
    }
}
