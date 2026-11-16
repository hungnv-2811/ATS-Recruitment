namespace ATS.Recruitment.Domain.Ports;

// Repository cua Recruitment (contracts.md 2.1, ban chot o muc 8.4).
// Entity tra ve tu Get*/List* (khong ghi "AsNoTracking") duoc EF theo doi: sua bang hanh vi cua
// entity roi goi IUnitOfWork.SaveChangesAsync — khong co phuong thuc Update rieng.
// Khong tra IQueryable (contracts.md muc 7).

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Job>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    /// <summary>Tin Open, loc + phan trang, moi nhat truoc.</summary>
    Task<(IReadOnlyList<Job> Items, int Total)> SearchOpenAsync(JobSearchCriteria criteria, CancellationToken ct = default);

    /// <summary>Moi tin cua mot HR, moi trang thai.</summary>
    Task<IReadOnlyList<Job>> ListByOwnerAsync(Guid hrOwnerId, CancellationToken ct = default);

    Task AddAsync(Job job, CancellationToken ct = default);
}

/// <param name="Search">Tim trong tieu de / mo ta / yeu cau (khong phan biet hoa thuong).</param>
public sealed record JobSearchCriteria(
    string? Search,
    string? Location,
    EmploymentType? EmploymentType,
    int Page,
    int PageSize);

public interface ICandidateRepository
{
    Task<Candidate?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<Candidate?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Candidate>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task AddAsync(Candidate candidate, CancellationToken ct = default);
}

public interface IHrProfileRepository
{
    Task<HrProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(HrProfile profile, CancellationToken ct = default);
}

public interface ICvRepository
{
    Task<Cv?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Cv>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<IReadOnlyList<Cv>> ListByCandidateAsync(Guid candidateId, CancellationToken ct = default);

    Task AddAsync(Cv cv, CancellationToken ct = default);

    Task DeleteAsync(Cv cv, CancellationToken ct = default);
}

public interface IApplicationRepository
{
    Task<Application?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Application>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<IReadOnlyList<Application>> ListByJobAsync(Guid jobId, ApplicationStatus? status = null, CancellationToken ct = default);

    Task<IReadOnlyList<Application>> ListByCandidateAsync(Guid candidateId, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid jobId, Guid candidateId, CancellationToken ct = default);

    /// <summary>CV nay da duoc dung de nop don nao chua (chan xoa CV).</summary>
    Task<bool> AnyUsingCvAsync(Guid cvId, CancellationToken ct = default);

    /// <summary>So don theo tin (tin khong co don thi khong co trong ket qua).</summary>
    Task<IReadOnlyDictionary<Guid, int>> CountByJobsAsync(IReadOnlyCollection<Guid> jobIds, CancellationToken ct = default);

    Task AddAsync(Application application, CancellationToken ct = default);
}

public interface IInterviewRepository
{
    Task<Interview?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Interview>> ListByApplicationAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>Buoi Scheduled tu <paramref name="since"/> tro di, cua cac tin do HR so huu.</summary>
    Task<IReadOnlyList<Interview>> ListUpcomingForHrAsync(Guid hrOwnerId, DateTimeOffset since, CancellationToken ct = default);

    Task AddAsync(Interview interview, CancellationToken ct = default);
}

public interface IEvaluationRepository
{
    Task<IReadOnlyList<Evaluation>> ListByInterviewsAsync(IReadOnlyCollection<Guid> interviewIds, CancellationToken ct = default);

    Task<bool> ExistsForInterviewAsync(Guid interviewId, CancellationToken ct = default);

    Task AddAsync(Evaluation evaluation, CancellationToken ct = default);
}
