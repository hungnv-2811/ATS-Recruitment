using ATS.AiScreening.Application.Ports;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Entities;
using ATS.AiScreening.Domain.Ports;
using ATS.AiScreening.Domain.Services;
using ATS.AiScreening.Infrastructure;
using ATS.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATS.AiScreening.Tests;

/// <summary>Tao AnonymizedCv DUNG duong that (SimpleAnonymizer) — test khong co cach nao khac, va do la dieu ta muon.</summary>
internal static class Cv
{
    private static readonly SimpleAnonymizer Anonymizer = new();

    public static Task<AnonymizedCv> AnonAsync(string text) => Anonymizer.AnonymizeAsync(text);

    public static JobRequirement Jd(
        string title = "Backend Developer",
        string[]? required = null,
        string[]? nice = null,
        int minYears = 0)
        => new(title, "Mo ta cong viec", required ?? ["C#", "PostgreSQL", "Docker"], nice ?? [], minYears);
}

internal static class Db
{
    /// <summary>AtsDbContext tren EF InMemory, nap dung cac IEntityTypeConfiguration cua AiScreening.</summary>
    public static AtsDbContext Create(string? name = null)
    {
        var options = new DbContextOptionsBuilder<AtsDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;
        return new AtsDbContext(options, new ModuleAssemblies(typeof(InfrastructureAssemblyMarker).Assembly));
    }
}

internal sealed class FakeRecruitmentReader : IRecruitmentReader
{
    public Dictionary<Guid, JobInfo> Jobs { get; } = [];

    public Dictionary<Guid, CvInfo> Cvs { get; } = [];

    public Dictionary<Guid, (Guid JobId, Guid CvId)> Applications { get; } = [];

    public Dictionary<Guid, Guid> InterviewToApplication { get; } = [];

    public JobInfo AddJob(Guid hrId, string requirements = "C#, PostgreSQL, Docker", string status = "Open")
    {
        var job = new JobInfo(Guid.NewGuid(), hrId, "Backend Developer", "Phat trien API", requirements, status);
        Jobs[job.JobId] = job;
        return job;
    }

    public CvInfo AddCv(Guid candidateId, string text = "5 năm kinh nghiệm C#, PostgreSQL, Docker.")
    {
        var cv = new CvInfo(Guid.NewGuid(), candidateId, "CV chinh", text);
        Cvs[cv.CvId] = cv;
        return cv;
    }

    public Guid AddApplication(JobInfo job, CvInfo cv)
    {
        var id = Guid.NewGuid();
        Applications[id] = (job.JobId, cv.CvId);
        return id;
    }

    public Task<JobInfo?> GetJobAsync(Guid jobId, CancellationToken ct = default)
        => Task.FromResult(Jobs.GetValueOrDefault(jobId));

    public Task<CvInfo?> GetCvAsync(Guid cvId, CancellationToken ct = default)
        => Task.FromResult(Cvs.GetValueOrDefault(cvId));

    public Task<ApplicationInfo?> GetApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => Task.FromResult(Applications.TryGetValue(applicationId, out var a)
            ? new ApplicationInfo(applicationId, "Applied", Jobs[a.JobId], Cvs[a.CvId])
            : null);

    public Task<Guid?> GetApplicationIdOfInterviewAsync(Guid interviewId, CancellationToken ct = default)
        => Task.FromResult(InterviewToApplication.TryGetValue(interviewId, out var id) ? id : (Guid?)null);

    public Task<IReadOnlyList<Guid>> ListApplicationIdsByJobAsync(Guid jobId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Guid>>(Applications.Where(a => a.Value.JobId == jobId).Select(a => a.Key).ToArray());
}

/// <summary>Han muc dem trong bo nho — de kiem "trung cache thi KHONG tru luot".</summary>
internal sealed class CountingQuota : IAiUsageQuota
{
    public CountingQuota(int previewLimit = 20, int batchLimit = 5)
    {
        PreviewLimit = previewLimit;
        BatchLimit = batchLimit;
    }

    public int PreviewLimit { get; }

    public int BatchLimit { get; }

    public int PreviewUsed { get; private set; }

    public int BatchUsed { get; private set; }

    public Task<bool> TryConsumePreviewAsync(Guid userId, CancellationToken ct = default)
    {
        if (PreviewUsed >= PreviewLimit)
        {
            return Task.FromResult(false);
        }

        PreviewUsed++;
        return Task.FromResult(true);
    }

    public Task<bool> TryConsumeBatchAsync(Guid userId, CancellationToken ct = default)
    {
        if (BatchUsed >= BatchLimit)
        {
            return Task.FromResult(false);
        }

        BatchUsed++;
        return Task.FromResult(true);
    }

    public Task<QuotaStatus> GetStatusAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(new QuotaStatus(PreviewUsed, PreviewLimit, BatchUsed, BatchLimit));
}

internal sealed class InMemoryCacheRepository : IAiScoreCacheRepository
{
    private readonly Dictionary<string, (Guid Id, CachedScore Score)> _rows = new(StringComparer.Ordinal);

    public int Hits { get; private set; }

    public int Count => _rows.Count;

    public Task<CachedScore?> GetAsync(CacheKey key, CancellationToken ct = default)
    {
        if (_rows.TryGetValue(key.Value, out var row))
        {
            Hits++;
            return Task.FromResult<CachedScore?>(row.Score);
        }

        return Task.FromResult<CachedScore?>(null);
    }

    public Task SaveAsync(CacheKey key, ScreeningResult result, string modelVersion, string promptVersion, CancellationToken ct = default)
    {
        _rows.TryAdd(key.Value, (Guid.NewGuid(), new CachedScore(
            result.Score, result.Summary, result.Strengths, result.Gaps, result.AdapterUsed, modelVersion, promptVersion, DateTimeOffset.UtcNow)));
        return Task.CompletedTask;
    }

    public Task<Guid?> FindIdAsync(CacheKey key, CancellationToken ct = default)
        => Task.FromResult(_rows.TryGetValue(key.Value, out var row) ? row.Id : (Guid?)null);
}

/// <summary>Adapter cham gia: dem so lan goi, hanh vi tuy bien.</summary>
internal sealed class StubScoring : IAiScoringService
{
    private readonly Func<CancellationToken, Task<ScreeningResult>> _behavior;

    public StubScoring(Func<CancellationToken, Task<ScreeningResult>> behavior) => _behavior = behavior;

    public int Calls { get; private set; }

    public static StubScoring Returning(string adapter, int score = 77)
        => new(_ => Task.FromResult(new ScreeningResult(new Score(score), $"tu {adapter}", [], [], adapter)));

    public static StubScoring Throwing()
        => new(_ => Task.FromException<ScreeningResult>(new InvalidOperationException("adapter hong")));

    public static StubScoring Hanging()
        => new(async ct =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("khong toi duoc day");
        });

    /// <summary>Adapter "xau": khong ton trong CancellationToken — pipeline van phai cat duoc.</summary>
    public static StubScoring IgnoringCancellation()
        => new(async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None);
            throw new InvalidOperationException("khong toi duoc day");
        });

    public Task<ScreeningResult> ScoreAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default)
    {
        Calls++;
        return _behavior(ct);
    }
}

internal sealed class InMemoryScoreRepository : IAiScoreRepository
{
    public Dictionary<Guid, AiScore> Rows { get; } = [];

    public Task<AiScore?> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => Task.FromResult(Rows.GetValueOrDefault(applicationId));

    public Task<IReadOnlyList<AiScore>> ListByApplicationsAsync(IReadOnlyCollection<Guid> applicationIds, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AiScore>>(Rows.Values.Where(r => applicationIds.Contains(r.ApplicationId)).ToArray());

    public Task<AiScore?> GetByCacheKeyAsync(string cacheKey, CancellationToken ct = default)
        => Task.FromResult<AiScore?>(null);

    public Task UpsertAsync(AiScore score, CancellationToken ct = default)
    {
        Rows[score.ApplicationId] = score;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryJobRepository : IScreeningJobRepository
{
    public Dictionary<Guid, ScreeningJob> Rows { get; } = [];

    public int ProgressCalls { get; private set; }

    public Task<ScreeningJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Rows.GetValueOrDefault(id));

    public Task<ScreeningJob?> GetLatestByJobAsync(Guid jobId, CancellationToken ct = default)
        => Task.FromResult(Rows.Values.Where(j => j.JobId == jobId).OrderByDescending(j => j.StartedAt).FirstOrDefault());

    public Task AddAsync(ScreeningJob job, CancellationToken ct = default)
    {
        Rows[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task UpdateProgressAsync(Guid id, int done, int failed, CancellationToken ct = default)
    {
        ProgressCalls++;
        Rows[id].RecordProgress(done, failed, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}
