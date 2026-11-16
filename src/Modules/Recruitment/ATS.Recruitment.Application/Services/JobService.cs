using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Domain;
using ATS.Recruitment.Domain.Ports;
using ATS.SharedKernel;

namespace ATS.Recruitment.Application.Services;

/// <summary>Dang / sua / dong tin; danh sach tin cong khai va tin cua HR.</summary>
public sealed class JobService : IJobService
{
    public const int MaxPageSize = 100;

    private readonly IJobRepository _jobs;
    private readonly IApplicationRepository _applications;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _clock;

    public JobService(IJobRepository jobs, IApplicationRepository applications, IUnitOfWork uow, TimeProvider clock)
    {
        _jobs = jobs;
        _applications = applications;
        _uow = uow;
        _clock = clock;
    }

    public async Task<Result<PagedResult<JobDto>>> SearchAsync(JobSearchQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        EmploymentType? employmentType = null;
        if (!string.IsNullOrWhiteSpace(query.EmploymentType))
        {
            if (!TryParseEmploymentType(query.EmploymentType, out var parsed))
            {
                return Result.Failure<PagedResult<JobDto>>(EmploymentTypeError);
            }

            employmentType = parsed;
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var (items, total) = await _jobs.SearchOpenAsync(
            new JobSearchCriteria(Trim(query.Search), Trim(query.Location), employmentType, page, pageSize), ct);

        var dtos = await ToDtosAsync(items, ct);
        return new PagedResult<JobDto>(dtos, total, page, pageSize);
    }

    public async Task<Result<JobDto>> GetAsync(Guid jobId, Guid? callerUserId, CancellationToken ct = default)
    {
        var job = await _jobs.GetByIdAsync(jobId, ct);

        // Tin chua mo / da dong chi chu tin xem duoc; nguoi khac thay nhu khong ton tai.
        if (job is null || (!job.IsOpen && job.HrOwnerId != callerUserId))
        {
            return Result.Failure<JobDto>(JobNotFound);
        }

        return await ToDtoAsync(job, ct);
    }

    public async Task<Result<IReadOnlyList<JobDto>>> ListMineAsync(Guid hrUserId, CancellationToken ct = default)
    {
        var jobs = await _jobs.ListByOwnerAsync(hrUserId, ct);
        return Result.Success(await ToDtosAsync(jobs, ct));
    }

    public async Task<Result<JobDto>> CreateAsync(Guid hrUserId, CreateJobRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryParseEmploymentType(request.EmploymentType, out var employmentType))
        {
            return Result.Failure<JobDto>(EmploymentTypeError);
        }

        var created = Job.Create(
            hrUserId,
            request.Title,
            request.Description,
            request.Requirements ?? string.Empty,
            request.Location ?? string.Empty,
            employmentType,
            request.Publish,
            _clock.GetUtcNow());
        if (created.IsFailure)
        {
            return Result.Failure<JobDto>(created.Error);
        }

        await _jobs.AddAsync(created.Value, ct);
        await _uow.SaveChangesAsync(ct);
        return ToDto(created.Value, 0);
    }

    public async Task<Result<JobDto>> UpdateAsync(Guid hrUserId, Guid jobId, UpdateJobRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var owned = await GetOwnedAsync(hrUserId, jobId, ct);
        if (owned.IsFailure)
        {
            return Result.Failure<JobDto>(owned.Error);
        }

        var job = owned.Value;
        var employmentType = job.EmploymentType;
        if (request.EmploymentType is not null && !TryParseEmploymentType(request.EmploymentType, out employmentType))
        {
            return Result.Failure<JobDto>(EmploymentTypeError);
        }

        var updated = job.Update(
            request.Title ?? job.Title,
            request.Description ?? job.Description,
            request.Requirements ?? job.Requirements,
            request.Location ?? job.Location,
            employmentType);
        if (updated.IsFailure)
        {
            return Result.Failure<JobDto>(updated.Error);
        }

        await _uow.SaveChangesAsync(ct);
        return await ToDtoAsync(job, ct);
    }

    public Task<Result> PublishAsync(Guid hrUserId, Guid jobId, CancellationToken ct = default)
        => ChangeAsync(hrUserId, jobId, job => job.Publish(_clock.GetUtcNow()), ct);

    public Task<Result> CloseAsync(Guid hrUserId, Guid jobId, CancellationToken ct = default)
        => ChangeAsync(hrUserId, jobId, job => job.Close(_clock.GetUtcNow()), ct);

    internal static bool TryParseEmploymentType(string? value, out EmploymentType employmentType)
        => Enum.TryParse(value?.Trim(), ignoreCase: true, out employmentType)
           && Enum.IsDefined(employmentType)
           && !int.TryParse(value, out _);

    internal static JobDto ToDto(Job job, int applicationCount) => new(
        job.Id,
        job.Title,
        job.Description,
        job.Requirements,
        job.Location,
        job.EmploymentType.ToString(),
        job.Status.ToString(),
        job.PostedAt,
        job.ClosedAt,
        applicationCount);

    private static readonly Error JobNotFound = Error.NotFound("Khong tim thay tin tuyen dung.");

    private static readonly Error EmploymentTypeError =
        Error.Validation("Hinh thuc lam viec phai la FullTime, PartTime, Internship hoac Contract.");

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<Result> ChangeAsync(Guid hrUserId, Guid jobId, Func<Job, Result> change, CancellationToken ct)
    {
        var owned = await GetOwnedAsync(hrUserId, jobId, ct);
        if (owned.IsFailure)
        {
            return owned;
        }

        var result = change(owned.Value);
        if (result.IsSuccess)
        {
            await _uow.SaveChangesAsync(ct);
        }

        return result;
    }

    private async Task<Result<Job>> GetOwnedAsync(Guid hrUserId, Guid jobId, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(jobId, ct);
        if (job is null)
        {
            return Result.Failure<Job>(JobNotFound);
        }

        return job.HrOwnerId == hrUserId
            ? job
            : Result.Failure<Job>(Error.Forbidden("Ban khong so huu tin nay."));
    }

    private async Task<JobDto> ToDtoAsync(Job job, CancellationToken ct)
        => (await ToDtosAsync([job], ct))[0];

    private async Task<IReadOnlyList<JobDto>> ToDtosAsync(IReadOnlyList<Job> jobs, CancellationToken ct)
    {
        if (jobs.Count == 0)
        {
            return [];
        }

        var counts = await _applications.CountByJobsAsync(jobs.Select(j => j.Id).ToList(), ct);
        return jobs.Select(j => ToDto(j, counts.GetValueOrDefault(j.Id))).ToList();
    }
}
