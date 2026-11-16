using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Entities;
using ATS.AiScreening.Domain.Ports;
using ATS.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATS.AiScreening.Infrastructure.Persistence;

/// <summary>Bang <c>ai_scores</c>. Upsert theo application_id (unique) — khong bao gio sinh dong thu hai.</summary>
public sealed class EfAiScoreRepository : IAiScoreRepository
{
    private readonly AtsDbContext _db;

    public EfAiScoreRepository(AtsDbContext db) => _db = db;

    public Task<AiScore?> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => _db.Set<AiScore>().FirstOrDefaultAsync(x => x.ApplicationId == applicationId, ct);

    public async Task<IReadOnlyList<AiScore>> ListByApplicationsAsync(IReadOnlyCollection<Guid> applicationIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(applicationIds);
        if (applicationIds.Count == 0)
        {
            return [];
        }

        var ids = applicationIds.Distinct().ToArray();
        return await _db.Set<AiScore>()
            .AsNoTracking()
            .Where(x => ids.Contains(x.ApplicationId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public Task<AiScore?> GetByCacheKeyAsync(string cacheKey, CancellationToken ct = default)
        => (from score in _db.Set<AiScore>().AsNoTracking()
            join cache in _db.Set<AiScoreCacheEntry>() on score.CacheId equals cache.Id
            where cache.CacheKey == cacheKey
            orderby score.UpdatedAt descending
            select score).FirstOrDefaultAsync(ct);

    public async Task UpsertAsync(AiScore score, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(score);

        var entry = _db.Entry(score);
        if (entry.State == EntityState.Detached)
        {
            var exists = await _db.Set<AiScore>().AnyAsync(x => x.Id == score.Id, ct).ConfigureAwait(false);
            if (exists)
            {
                _db.Update(score);
            }
            else
            {
                _db.Add(score);
            }
        }

        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException) when (entry.State == EntityState.Added)
        {
            // Hai luong cung tao diem cho mot don (nop don + sang loc lo dua nhau) → unique application_id
            // chan dong thu hai. Chep gia tri vao dong da co thay vi lam hong ca request.
            var values = entry.CurrentValues.Clone();
            entry.State = EntityState.Detached;

            var existing = await _db.Set<AiScore>()
                .FirstAsync(x => x.ApplicationId == score.ApplicationId, ct)
                .ConfigureAwait(false);
            var existingEntry = _db.Entry(existing);
            foreach (var property in existingEntry.Metadata.GetProperties().Where(p => !p.IsPrimaryKey()))
            {
                existingEntry.Property(property.Name).CurrentValue = values[property.Name];
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}

/// <summary>Bang <c>screening_jobs</c>. Tien do cong bang mot UPDATE nguyen tu tren DB quan he.</summary>
public sealed class EfScreeningJobRepository : IScreeningJobRepository
{
    private readonly AtsDbContext _db;
    private readonly TimeProvider _clock;

    public EfScreeningJobRepository(AtsDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public Task<ScreeningJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Set<ScreeningJob>().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<ScreeningJob?> GetLatestByJobAsync(Guid jobId, CancellationToken ct = default)
        => _db.Set<ScreeningJob>()
            .AsNoTracking()
            .Where(x => x.JobId == jobId)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(ScreeningJob job, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        _db.Add(job);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateProgressAsync(Guid id, int done, int failed, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(done);
        ArgumentOutOfRangeException.ThrowIfNegative(failed);
        var now = _clock.GetUtcNow();

        if (_db.Database.IsRelational())
        {
            // Nhieu worker song song: doc-sua-ghi se mat dem. Mot cau UPDATE ... SET x = x + n thi khong.
            // Moi ve phai SET dung gia tri CU cua cot (chuan SQL), nen dieu kien Done tinh tren gia tri cu + delta.
            await _db.Set<ScreeningJob>()
                .Where(j => j.Id == id
                            && j.Status != ScreeningJobStatus.Done
                            && j.Status != ScreeningJobStatus.Cancelled)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(j => j.DoneCount, j => j.DoneCount + done)
                        .SetProperty(j => j.FailedCount, j => j.FailedCount + failed)
                        .SetProperty(
                            j => j.Status,
                            j => j.DoneCount + done + j.FailedCount + failed >= j.TargetCount
                                ? ScreeningJobStatus.Done
                                : ScreeningJobStatus.Running)
                        .SetProperty(
                            j => j.FinishedAt,
                            j => j.DoneCount + done + j.FailedCount + failed >= j.TargetCount
                                ? (DateTimeOffset?)now
                                : j.FinishedAt),
                    ct)
                .ConfigureAwait(false);
            return;
        }

        // Provider khong quan he (EF InMemory trong test): di qua hanh vi cua entity.
        var job = await _db.Set<ScreeningJob>().FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (job is null)
        {
            return;
        }

        job.RecordProgress(done, failed, now);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>Bang <c>interview_questions</c>.</summary>
public sealed class EfInterviewQuestionRepository : IInterviewQuestionRepository
{
    private readonly AtsDbContext _db;

    public EfInterviewQuestionRepository(AtsDbContext db) => _db = db;

    public async Task<IReadOnlyList<InterviewQuestionEntry>> ListByInterviewAsync(Guid interviewId, CancellationToken ct = default)
        => await _db.Set<InterviewQuestionEntry>()
            .AsNoTracking()
            .Where(x => x.InterviewId == interviewId)
            .OrderBy(x => x.OrderIndex)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task ReplaceForInterviewAsync(Guid interviewId, IReadOnlyList<InterviewQuestionEntry> questions, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(questions);

        var old = await _db.Set<InterviewQuestionEntry>()
            .Where(x => x.InterviewId == interviewId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Xoa cu + them moi trong CUNG mot SaveChanges = mot transaction.
        _db.RemoveRange(old);
        _db.AddRange(questions);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>Bang <c>ai_score_cache</c> — tra theo noi dung, khong theo don.</summary>
public sealed class EfAiScoreCacheRepository : IAiScoreCacheRepository
{
    private readonly AtsDbContext _db;
    private readonly TimeProvider _clock;

    public EfAiScoreCacheRepository(AtsDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<CachedScore?> GetAsync(CacheKey key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var now = _clock.GetUtcNow();

        if (_db.Database.IsRelational())
        {
            var found = await _db.Set<AiScoreCacheEntry>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CacheKey == key.Value, ct)
                .ConfigureAwait(false);
            if (found is null)
            {
                return null;
            }

            // Tang dem nguyen tu — nhieu request trung cung luc khong mat luot nao (so lieu RB2).
            await _db.Set<AiScoreCacheEntry>()
                .Where(x => x.Id == found.Id)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(x => x.HitCount, x => x.HitCount + 1)
                        .SetProperty(x => x.LastHitAt, (DateTimeOffset?)now),
                    ct)
                .ConfigureAwait(false);
            return found.ToCachedScore();
        }

        var entry = await _db.Set<AiScoreCacheEntry>()
            .FirstOrDefaultAsync(x => x.CacheKey == key.Value, ct)
            .ConfigureAwait(false);
        if (entry is null)
        {
            return null;
        }

        entry.RegisterHit(now);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.ToCachedScore();
    }

    public async Task SaveAsync(CacheKey key, ScreeningResult result, string modelVersion, string promptVersion, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(result);

        var exists = await _db.Set<AiScoreCacheEntry>().AnyAsync(x => x.CacheKey == key.Value, ct).ConfigureAwait(false);
        if (exists)
        {
            return;
        }

        var entry = AiScoreCacheEntry.Create(key, result, modelVersion, promptVersion, _clock.GetUtcNow());
        _db.Add(entry);
        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // Request khac vua ghi cung khoa (unique cache_key). Ket qua tuong duong — bo qua.
            _db.Entry(entry).State = EntityState.Detached;
        }
    }

    public Task<Guid?> FindIdAsync(CacheKey key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _db.Set<AiScoreCacheEntry>()
            .AsNoTracking()
            .Where(x => x.CacheKey == key.Value)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);
    }
}
