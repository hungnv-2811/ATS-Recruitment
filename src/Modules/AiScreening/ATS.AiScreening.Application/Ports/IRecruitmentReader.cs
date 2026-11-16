namespace ATS.AiScreening.Application.Ports;

/// <summary>
/// Cong DOC du lieu tuyen dung cua RIENG AiScreening.Application.
/// </summary>
/// <remarks>
/// Adapter o AiScreening.Infrastructure goi <c>IRecruitmentQueries</c> cua Recruitment.Application.
/// Nho lop boc nay, Application cua AiScreening KHONG tham chieu project nao cua Recruitment
/// (ArchitectureTests ep), va doi contract ben kia chi phai sua mot adapter.
/// <see cref="CvInfo.ExtractedText"/> la CV THO con PII — chi duoc dua vao IAnonymizer.
/// </remarks>
public interface IRecruitmentReader
{
    Task<JobInfo?> GetJobAsync(Guid jobId, CancellationToken ct = default);

    Task<CvInfo?> GetCvAsync(Guid cvId, CancellationToken ct = default);

    Task<ApplicationInfo?> GetApplicationAsync(Guid applicationId, CancellationToken ct = default);

    Task<Guid?> GetApplicationIdOfInterviewAsync(Guid interviewId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> ListApplicationIdsByJobAsync(Guid jobId, CancellationToken ct = default);
}

/// <param name="HrOwnerUserId">Id trong identity.users cua HR so huu tin.</param>
/// <param name="Status">"Draft" | "Open" | "Closed".</param>
public sealed record JobInfo(
    Guid JobId,
    Guid HrOwnerUserId,
    string Title,
    string Description,
    string Requirements,
    string Status)
{
    public bool IsOpen => string.Equals(Status, "Open", StringComparison.OrdinalIgnoreCase);
}

/// <param name="CandidateUserId">Id trong identity.users cua ung vien so huu CV.</param>
/// <param name="ExtractedText">CV THO (con PII). Rong khi khong trich duoc chu tu file.</param>
public sealed record CvInfo(
    Guid CvId,
    Guid CandidateUserId,
    string Name,
    string ExtractedText);

public sealed record ApplicationInfo(
    Guid ApplicationId,
    string Status,
    JobInfo Job,
    CvInfo Cv);
