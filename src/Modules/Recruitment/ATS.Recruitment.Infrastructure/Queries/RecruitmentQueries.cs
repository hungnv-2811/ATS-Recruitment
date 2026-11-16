using ATS.Persistence;
using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Domain;
using Microsoft.EntityFrameworkCore;
using JobApplication = ATS.Recruitment.Domain.Application;

namespace ATS.Recruitment.Infrastructure.Queries;

/// <summary>
/// Cua doc cho AiScreening (contracts.md 8.2): chi tra snapshot bat bien, AsNoTracking.
/// </summary>
internal sealed class RecruitmentQueries : IRecruitmentQueries
{
    private readonly AtsDbContext _db;

    public RecruitmentQueries(AtsDbContext db) => _db = db;

    public async Task<ApplicationSnapshot?> GetApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var application = await _db.Set<JobApplication>().AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => new { a.Id, a.Status, a.JobId, a.CvId })
            .FirstOrDefaultAsync(ct);
        if (application is null)
        {
            return null;
        }

        var job = await GetJobAsync(application.JobId, ct);
        var cv = await GetCvAsync(application.CvId, ct);
        return job is null || cv is null
            ? null
            : new ApplicationSnapshot(application.Id, application.Status.ToString(), job, cv);
    }

    public async Task<JobSnapshot?> GetJobAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _db.Set<Job>().AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
        return job is null
            ? null
            : new JobSnapshot(job.Id, job.HrOwnerId, job.Title, job.Description, job.Requirements, job.Status.ToString());
    }

    public Task<CvSnapshot?> GetCvAsync(Guid cvId, CancellationToken ct = default)
        => (from cv in _db.Set<Cv>().AsNoTracking()
            join c in _db.Set<Candidate>() on cv.CandidateId equals c.Id
            where cv.Id == cvId
            select new CvSnapshot(cv.Id, c.UserId, cv.Name, cv.ExtractedText))
            .FirstOrDefaultAsync(ct);

    public Task<Guid?> GetApplicationIdOfInterviewAsync(Guid interviewId, CancellationToken ct = default)
        => _db.Set<Interview>().AsNoTracking()
            .Where(i => i.Id == interviewId)
            .Select(i => (Guid?)i.ApplicationId)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Guid>> ListApplicationIdsByJobAsync(Guid jobId, CancellationToken ct = default)
        => await _db.Set<JobApplication>().AsNoTracking()
            .Where(a => a.JobId == jobId)
            .OrderBy(a => a.AppliedAt)
            .Select(a => a.Id)
            .ToListAsync(ct);
}
