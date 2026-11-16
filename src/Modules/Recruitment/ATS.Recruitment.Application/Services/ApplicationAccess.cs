using ATS.Recruitment.Domain;
using ATS.Recruitment.Domain.Ports;
using ATS.SharedKernel;
using JobApplication = ATS.Recruitment.Domain.Application;

namespace ATS.Recruitment.Application.Services;

/// <summary>Don + tin + ung vien da nap, kem vai tro cua nguoi dang goi doi voi don.</summary>
internal sealed record ApplicationContext(
    JobApplication Application,
    Job Job,
    Candidate Candidate,
    bool IsHrOwner);

/// <summary>Kiem quyen dung chung: "HR chu tin" va "ung vien chu don" (RB8).</summary>
internal sealed class ApplicationAccess
{
    private readonly IApplicationRepository _applications;
    private readonly IJobRepository _jobs;
    private readonly ICandidateRepository _candidates;

    public ApplicationAccess(IApplicationRepository applications, IJobRepository jobs, ICandidateRepository candidates)
    {
        _applications = applications;
        _jobs = jobs;
        _candidates = candidates;
    }

    /// <param name="callerUserId">Claim "sub" cua nguoi goi.</param>
    /// <param name="applicationId">Don can truy cap.</param>
    /// <param name="hrOwnerOnly">true: chi HR chu tin; false: HR chu tin HOAC ung vien chu don.</param>
    /// <param name="ct">Token huy.</param>
    public async Task<Result<ApplicationContext>> LoadAsync(Guid callerUserId, Guid applicationId, bool hrOwnerOnly, CancellationToken ct)
    {
        var application = await _applications.GetByIdAsync(applicationId, ct);
        if (application is null)
        {
            return Result.Failure<ApplicationContext>(Errors.ApplicationNotFound);
        }

        var job = await _jobs.GetByIdAsync(application.JobId, ct);
        var candidate = await _candidates.GetByIdAsync(application.CandidateId, ct);
        if (job is null || candidate is null)
        {
            return Result.Failure<ApplicationContext>(Errors.ApplicationNotFound);
        }

        var isHrOwner = job.HrOwnerId == callerUserId;
        var isCandidateOwner = candidate.UserId == callerUserId;

        if (isHrOwner || (!hrOwnerOnly && isCandidateOwner))
        {
            return new ApplicationContext(application, job, candidate, isHrOwner);
        }

        return Result.Failure<ApplicationContext>(
            hrOwnerOnly ? Errors.NotJobOwner : Error.Forbidden("Ban khong co quyen xem don nay."));
    }
}
