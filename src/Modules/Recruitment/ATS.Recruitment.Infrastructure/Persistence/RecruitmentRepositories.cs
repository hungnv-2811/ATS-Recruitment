using ATS.Persistence;
using ATS.Recruitment.Domain;
using ATS.Recruitment.Domain.Ports;
using Microsoft.EntityFrameworkCore;
using JobApplication = ATS.Recruitment.Domain.Application;

namespace ATS.Recruitment.Infrastructure.Persistence;

// Repository tren AtsDbContext dung chung. Khong goi SaveChanges — Application quyet dinh ranh
// gioi transaction qua IUnitOfWork. Truy van chi dung toan tu dich duoc ca tren Npgsql lan
// provider InMemory (test): ToLower().Contains thay vi ILike.

internal sealed class JobRepository : IJobRepository
{
    private readonly AtsDbContext _db;

    public JobRepository(AtsDbContext db) => _db = db;

    public Task<Job?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Set<Job>().FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<IReadOnlyList<Job>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => await _db.Set<Job>().Where(j => ids.Contains(j.Id)).ToListAsync(ct);

    public async Task<(IReadOnlyList<Job> Items, int Total)> SearchOpenAsync(JobSearchCriteria criteria, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = _db.Set<Job>().AsNoTracking().Where(j => j.Status == JobStatus.Open);

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = criteria.Search.ToLowerInvariant();
            query = query.Where(j =>
                j.Title.ToLower().Contains(term)
                || j.Description.ToLower().Contains(term)
                || j.Requirements.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Location))
        {
            var location = criteria.Location.ToLowerInvariant();
            query = query.Where(j => j.Location.ToLower().Contains(location));
        }

        if (criteria.EmploymentType is { } employmentType)
        {
            query = query.Where(j => j.EmploymentType == employmentType);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(j => j.PostedAt)
            .ThenByDescending(j => j.CreatedAt)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<Job>> ListByOwnerAsync(Guid hrOwnerId, CancellationToken ct = default)
        => await _db.Set<Job>().AsNoTracking()
            .Where(j => j.HrOwnerId == hrOwnerId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Job job, CancellationToken ct = default)
        => await _db.Set<Job>().AddAsync(job, ct);
}

internal sealed class CandidateRepository : ICandidateRepository
{
    private readonly AtsDbContext _db;

    public CandidateRepository(AtsDbContext db) => _db = db;

    public Task<Candidate?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.Set<Candidate>().FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public Task<Candidate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Set<Candidate>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Candidate>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => await _db.Set<Candidate>().AsNoTracking().Where(c => ids.Contains(c.Id)).ToListAsync(ct);

    public async Task AddAsync(Candidate candidate, CancellationToken ct = default)
        => await _db.Set<Candidate>().AddAsync(candidate, ct);
}

internal sealed class HrProfileRepository : IHrProfileRepository
{
    private readonly AtsDbContext _db;

    public HrProfileRepository(AtsDbContext db) => _db = db;

    public Task<HrProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.Set<HrProfile>().FirstOrDefaultAsync(h => h.UserId == userId, ct);

    public async Task AddAsync(HrProfile profile, CancellationToken ct = default)
        => await _db.Set<HrProfile>().AddAsync(profile, ct);
}

internal sealed class CvRepository : ICvRepository
{
    private readonly AtsDbContext _db;

    public CvRepository(AtsDbContext db) => _db = db;

    public Task<Cv?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Set<Cv>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Cv>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => await _db.Set<Cv>().AsNoTracking().Where(c => ids.Contains(c.Id)).ToListAsync(ct);

    public async Task<IReadOnlyList<Cv>> ListByCandidateAsync(Guid candidateId, CancellationToken ct = default)
        => await _db.Set<Cv>()
            .Where(c => c.CandidateId == candidateId)
            .OrderByDescending(c => c.UploadedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Cv cv, CancellationToken ct = default)
        => await _db.Set<Cv>().AddAsync(cv, ct);

    public Task DeleteAsync(Cv cv, CancellationToken ct = default)
    {
        _db.Set<Cv>().Remove(cv);
        return Task.CompletedTask;
    }
}

internal sealed class ApplicationRepository : IApplicationRepository
{
    private readonly AtsDbContext _db;

    public ApplicationRepository(AtsDbContext db) => _db = db;

    public Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Set<JobApplication>().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<JobApplication>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => await _db.Set<JobApplication>().AsNoTracking().Where(a => ids.Contains(a.Id)).ToListAsync(ct);

    public async Task<IReadOnlyList<JobApplication>> ListByJobAsync(Guid jobId, ApplicationStatus? status = null, CancellationToken ct = default)
    {
        var query = _db.Set<JobApplication>().AsNoTracking().Where(a => a.JobId == jobId);
        if (status is { } s)
        {
            query = query.Where(a => a.Status == s);
        }

        return await query.OrderBy(a => a.AppliedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<JobApplication>> ListByCandidateAsync(Guid candidateId, CancellationToken ct = default)
        => await _db.Set<JobApplication>().AsNoTracking()
            .Where(a => a.CandidateId == candidateId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(Guid jobId, Guid candidateId, CancellationToken ct = default)
        => _db.Set<JobApplication>().AnyAsync(a => a.JobId == jobId && a.CandidateId == candidateId, ct);

    public Task<bool> AnyUsingCvAsync(Guid cvId, CancellationToken ct = default)
        => _db.Set<JobApplication>().AnyAsync(a => a.CvId == cvId, ct);

    public async Task<IReadOnlyDictionary<Guid, int>> CountByJobsAsync(IReadOnlyCollection<Guid> jobIds, CancellationToken ct = default)
        => await _db.Set<JobApplication>()
            .Where(a => jobIds.Contains(a.JobId))
            .GroupBy(a => a.JobId)
            .Select(g => new { JobId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.JobId, x => x.Count, ct);

    public async Task AddAsync(JobApplication application, CancellationToken ct = default)
        => await _db.Set<JobApplication>().AddAsync(application, ct);
}

internal sealed class InterviewRepository : IInterviewRepository
{
    private readonly AtsDbContext _db;

    public InterviewRepository(AtsDbContext db) => _db = db;

    public Task<Interview?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Set<Interview>().FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<Interview>> ListByApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => await _db.Set<Interview>().AsNoTracking()
            .Where(i => i.ApplicationId == applicationId)
            .OrderBy(i => i.ScheduledAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Interview>> ListUpcomingForHrAsync(Guid hrOwnerId, DateTimeOffset since, CancellationToken ct = default)
        => await (
                from i in _db.Set<Interview>().AsNoTracking()
                join a in _db.Set<JobApplication>() on i.ApplicationId equals a.Id
                join j in _db.Set<Job>() on a.JobId equals j.Id
                where j.HrOwnerId == hrOwnerId
                      && i.Status == InterviewStatus.Scheduled
                      && i.ScheduledAt >= since
                orderby i.ScheduledAt
                select i)
            .ToListAsync(ct);

    public async Task AddAsync(Interview interview, CancellationToken ct = default)
        => await _db.Set<Interview>().AddAsync(interview, ct);
}

internal sealed class EvaluationRepository : IEvaluationRepository
{
    private readonly AtsDbContext _db;

    public EvaluationRepository(AtsDbContext db) => _db = db;

    public async Task<IReadOnlyList<Evaluation>> ListByInterviewsAsync(IReadOnlyCollection<Guid> interviewIds, CancellationToken ct = default)
        => await _db.Set<Evaluation>().AsNoTracking().Where(e => interviewIds.Contains(e.InterviewId)).ToListAsync(ct);

    public Task<bool> ExistsForInterviewAsync(Guid interviewId, CancellationToken ct = default)
        => _db.Set<Evaluation>().AnyAsync(e => e.InterviewId == interviewId, ct);

    public async Task AddAsync(Evaluation evaluation, CancellationToken ct = default)
        => await _db.Set<Evaluation>().AddAsync(evaluation, ct);
}
