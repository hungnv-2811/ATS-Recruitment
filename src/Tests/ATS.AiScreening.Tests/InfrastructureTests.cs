using ATS.AiScreening.Application;
using ATS.AiScreening.Application.Abstractions;
using ATS.AiScreening.Application.Handlers;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Entities;
using ATS.AiScreening.Domain.Ports;
using ATS.AiScreening.Infrastructure;
using ATS.AiScreening.Infrastructure.CrossModule;
using ATS.AiScreening.Infrastructure.Persistence;
using ATS.AiScreening.Infrastructure.Queue;
using ATS.AiScreening.Infrastructure.Scoring;
using ATS.Persistence;
using ATS.Recruitment.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ATS.AiScreening.Tests;

/// <summary>Repository + quota tren EF InMemory (duong "provider khac Npgsql" cua adapter).</summary>
public sealed class EfRepositoryTests
{
    [Fact]
    public async Task Quota_preview_dung_tran_roi_tu_choi()
    {
        using var db = Db.Create();
        var quota = new EfAiUsageQuota(db, new AiScreeningOptions { PreviewLimitPerDay = 2 }, TimeProvider.System);
        var user = Guid.NewGuid();

        Assert.True(await quota.TryConsumePreviewAsync(user));
        Assert.True(await quota.TryConsumePreviewAsync(user));
        Assert.False(await quota.TryConsumePreviewAsync(user));

        var status = await quota.GetStatusAsync(user);
        Assert.Equal(2, status.PreviewUsedToday);
        Assert.Equal(0, status.PreviewRemainingToday);
        Assert.Equal(0, status.BatchUsedToday);
    }

    [Fact]
    public async Task Quota_tach_rieng_tung_user_va_tung_loai()
    {
        using var db = Db.Create();
        var quota = new EfAiUsageQuota(db, new AiScreeningOptions { PreviewLimitPerDay = 1, BatchLimitPerDay = 1 }, TimeProvider.System);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        Assert.True(await quota.TryConsumePreviewAsync(a));
        Assert.True(await quota.TryConsumeBatchAsync(a));
        Assert.True(await quota.TryConsumePreviewAsync(b));
        Assert.False(await quota.TryConsumeBatchAsync(a));
        Assert.Single(db.Set<AiUsageQuota>().Where(q => q.UserId == a));
    }

    [Fact]
    public async Task Cache_trung_thi_tang_hit_count()
    {
        using var db = Db.Create();
        var cache = new EfAiScoreCacheRepository(db, TimeProvider.System);
        var key = CacheKey.From("cv", "jd", "gpt-4o-mini", "scoring-v1");
        var result = new ScreeningResult(new Score(72), "tot", ["C#"], ["Docker"], "OpenAI");

        Assert.Null(await cache.GetAsync(key));
        await cache.SaveAsync(key, result, "gpt-4o-mini", "scoring-v1");
        await cache.SaveAsync(key, result, "gpt-4o-mini", "scoring-v1"); // ghi trung khoa: bo qua, khong loi
        var hit = await cache.GetAsync(key);
        await cache.GetAsync(key);

        Assert.NotNull(hit);
        Assert.Equal(72, hit.Score.Value);
        Assert.Equal(["C#"], hit.Strengths);
        var row = Assert.Single(db.Set<AiScoreCacheEntry>());
        Assert.Equal(2, row.HitCount);
        Assert.NotNull(row.LastHitAt);
        Assert.Equal(row.Id, await cache.FindIdAsync(key));
    }

    [Fact]
    public async Task Upsert_AiScore_khong_sinh_dong_thu_hai()
    {
        var name = Guid.NewGuid().ToString();
        var applicationId = Guid.NewGuid();

        using (var db = Db.Create(name))
        {
            await new EfAiScoreRepository(db).UpsertAsync(AiScore.CreatePending(applicationId, DateTimeOffset.UtcNow));
        }

        using (var db = Db.Create(name))
        {
            var repo = new EfAiScoreRepository(db);
            var score = await repo.GetByApplicationAsync(applicationId);
            score!.MarkScored(new ScreeningResult(new Score(90), "x", ["a", "b"], [], "Fake"), "fake-v1", null, DateTimeOffset.UtcNow);
            await repo.UpsertAsync(score);
        }

        using (var db = Db.Create(name))
        {
            var row = Assert.Single(db.Set<AiScore>());
            Assert.Equal(90, row.Score);
            Assert.Equal(ScreeningStatus.Scored, row.ScreeningStatus);
            Assert.Equal(["a", "b"], row.Strengths);
        }
    }

    [Fact]
    public async Task Tien_do_lo_cong_don_va_tu_Done()
    {
        using var db = Db.Create();
        var repo = new EfScreeningJobRepository(db, TimeProvider.System);
        var job = ScreeningJob.Start(Guid.NewGuid(), Guid.NewGuid(), 2, DateTimeOffset.UtcNow);
        await repo.AddAsync(job);

        await repo.UpdateProgressAsync(job.Id, done: 1, failed: 0);
        await repo.UpdateProgressAsync(job.Id, done: 0, failed: 1);

        var saved = await repo.GetByIdAsync(job.Id);
        Assert.Equal(1, saved!.DoneCount);
        Assert.Equal(1, saved.FailedCount);
        Assert.Equal(ScreeningJobStatus.Done, saved.Status);
        Assert.Equal(job.Id, (await repo.GetLatestByJobAsync(job.JobId))!.Id);
    }

    [Fact]
    public async Task Score_reader_tra_kieu_nguyen_thuy_cho_Recruitment()
    {
        using var db = Db.Create();
        var repo = new EfAiScoreRepository(db);
        var scored = AiScore.CreatePending(Guid.NewGuid(), DateTimeOffset.UtcNow);
        scored.MarkScored(new ScreeningResult(new Score(55), "tb", [], [], "Keyword"), "x", null, DateTimeOffset.UtcNow);
        await repo.UpsertAsync(scored);
        var unknown = Guid.NewGuid();

        var map = await new ApplicationScoreReader(repo).GetScoresAsync([scored.ApplicationId, unknown]);

        var entry = Assert.Single(map);
        Assert.Equal(55, entry.Value.Score);
        Assert.Equal("Keyword", entry.Value.AdapterUsed);
        Assert.Equal("Scored", entry.Value.ScreeningStatus);
    }

    [Fact]
    public async Task Trigger_ghi_Pending_va_xep_hang_doi_cham_le()
    {
        using var db = Db.Create();
        var queue = new InMemoryScreeningQueue();
        var repo = new EfAiScoreRepository(db);
        var trigger = new ScreeningTriggerAdapter(repo, queue, TimeProvider.System, NullLogger<ScreeningTriggerAdapter>.Instance);
        var applicationId = Guid.NewGuid();

        await trigger.RequestScreeningAsync(applicationId);

        Assert.Equal(ScreeningStatus.Pending, (await repo.GetByApplicationAsync(applicationId))!.ScreeningStatus);
        var task = await queue.DequeueAsync();
        Assert.Equal(new ScreeningTask(applicationId, Guid.Empty), task);
        Assert.Null(await queue.DequeueAsync());
    }
}

/// <summary>AddAiScreeningModule phai resolve duoc moi service ma KHONG can Postgres/Redis/OpenAI that.</summary>
public sealed class ModuleRegistrationTests
{
    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new ModuleAssemblies(typeof(InfrastructureAssemblyMarker).Assembly));
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<AtsDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddAiScreeningModule(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void AiProvider_Fake_chi_dung_FakeAdapter()
    {
        using var provider = Build(new() { ["AiProvider"] = "Fake" });

        Assert.IsType<FakeAiScoringAdapter>(provider.GetRequiredService<IAiScoringService>());
        Assert.IsType<InMemoryScreeningQueue>(provider.GetRequiredService<IScreeningQueue>());
        Assert.Equal("fake-v1", provider.GetRequiredService<AiScreeningOptions>().ModelVersion);
    }

    [Fact]
    public async Task OpenAI_khong_co_khoa_thi_pipeline_chi_con_Keyword()
    {
        using var provider = Build(new() { ["AiProvider"] = "OpenAI", ["OpenAI:ApiKey"] = "" });

        var scoring = provider.GetRequiredService<IAiScoringService>();
        var result = await scoring.ScoreAsync(await Cv.AnonAsync("C#"), Cv.Jd());

        Assert.IsType<AiScoringPipeline>(scoring);
        Assert.Equal(AdapterNames.Keyword, result.AdapterUsed);
    }

    [Fact]
    public void Co_Redis_thi_dung_RedisQueue_nhung_khong_ket_noi_luc_khoi_dong()
    {
        // Cong 1 khong co ai nghe: neu ket noi ngay luc resolve thi test nay se treo/loi.
        using var provider = Build(new() { ["Redis:ConnectionString"] = "127.0.0.1:1,connectTimeout=100" });

        Assert.IsType<RedisScreeningQueue>(provider.GetRequiredService<IScreeningQueue>());
    }

    [Fact]
    public void Ghi_de_adapter_cross_module_va_resolve_moi_handler()
    {
        using var provider = Build(new() { ["AiProvider"] = "Fake", ["Ai:PreviewLimitPerDay"] = "7" });
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.IsType<ScreeningTriggerAdapter>(sp.GetRequiredService<IApplicationScreeningTrigger>());
        Assert.IsType<ApplicationScoreReader>(sp.GetRequiredService<IApplicationScoreReader>());
        Assert.IsType<ProcessScreeningHandler>(sp.GetRequiredService<IProcessScreeningHandler>());
        Assert.NotNull(sp.GetRequiredService<PreviewScoreHandler>());
        Assert.NotNull(sp.GetRequiredService<StartScreeningHandler>());
        Assert.NotNull(sp.GetRequiredService<GenerateQuestionsHandler>());
        Assert.NotNull(sp.GetRequiredService<InterviewQuestionsHandler>());
        Assert.Equal(7, sp.GetRequiredService<AiScreeningOptions>().PreviewLimitPerDay);
    }
}

/// <summary>
/// Goi OpenAI THAT — chi chay o workflow ai-smoke (thu cong / hang tuan). Khong co OPENAI_API_KEY thi
/// return som va coi nhu pass: CI thuong khong ton tien, khong do oan (ai-integration.md muc 6).
/// </summary>
[Trait("Category", "AiSmoke")]
public sealed class AiSmokeTests
{
    private static string? ApiKey => Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    private static ServiceProvider BuildReal()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiProvider"] = "OpenAI",
                ["OpenAI:ApiKey"] = ApiKey,
                ["OpenAI:Model"] = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini",
                ["Ai:PreviewTimeoutSeconds"] = "60",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new ModuleAssemblies(typeof(InfrastructureAssemblyMarker).Assembly));
        services.AddDbContext<AtsDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddAiScreeningModule(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task OpenAI_that_cham_duoc_mot_cap_CV_JD()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            return;
        }

        using var provider = BuildReal();
        var adapter = provider.GetRequiredService<OpenAiScoringAdapter>();
        var cv = await Cv.AnonAsync("4 năm kinh nghiệm C#, ASP.NET Core, PostgreSQL, Docker. Email: a@b.com");

        var result = await adapter.ScoreAsync(cv, Cv.Jd(minYears: 3));

        Assert.Equal(AdapterNames.OpenAi, result.AdapterUsed);
        Assert.InRange(result.Score.Value, 0, 100);
        Assert.False(string.IsNullOrWhiteSpace(result.Summary));
    }

    [Fact]
    public async Task OpenAI_that_sinh_duoc_cau_hoi_phong_van()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            return;
        }

        using var provider = BuildReal();
        var generator = provider.GetRequiredService<OpenAiInterviewQuestionAdapter>();

        var questions = await generator.GenerateAsync(await Cv.AnonAsync("3 năm C#, Docker"), Cv.Jd());

        Assert.NotEmpty(questions);
        Assert.All(questions, q => Assert.Equal("AI", q.Source));
    }
}
