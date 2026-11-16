namespace ATS.Recruitment.Application.Contracts;

/// <summary>
/// Cua DOC duy nhat ma module khac (AiScreening) duoc dung de lay du lieu tuyen dung.
/// </summary>
/// <remarks>
/// Contract liên module chot o tuan 3 (xem docs/contracts.md muc 6).
/// TV2 hien thuc trong Recruitment.Infrastructure; AiScreening.Infrastructure goi qua
/// adapter cua minh. Chi tra snapshot bat bien — khong tra entity, khong tra IQueryable.
/// <c>ExtractedText</c> la CV THO (con PII): ben AiScreening BAT BUOC dua qua IAnonymizer
/// truoc khi toi bat ky port AI nao (ADR-3).
/// </remarks>
public interface IRecruitmentQueries
{
    Task<ApplicationSnapshot?> GetApplicationAsync(Guid applicationId, CancellationToken ct = default);

    Task<JobSnapshot?> GetJobAsync(Guid jobId, CancellationToken ct = default);

    Task<CvSnapshot?> GetCvAsync(Guid cvId, CancellationToken ct = default);

    /// <summary>Don ung tuyen ma buoi phong van thuoc ve — de AiScreening kiem quyen khi luu cau hoi.</summary>
    Task<Guid?> GetApplicationIdOfInterviewAsync(Guid interviewId, CancellationToken ct = default);

    /// <summary>Moi don cua mot tin (ke ca da Rejected) — dung cho sang loc lo.</summary>
    Task<IReadOnlyList<Guid>> ListApplicationIdsByJobAsync(Guid jobId, CancellationToken ct = default);
}

/// <param name="HrOwnerUserId">Id trong identity.users cua HR so huu tin (de kiem tra quyen).</param>
public sealed record JobSnapshot(
    Guid JobId,
    Guid HrOwnerUserId,
    string Title,
    string Description,
    string Requirements,
    string Status);

/// <param name="CandidateUserId">Id trong identity.users cua ung vien so huu CV.</param>
/// <param name="ExtractedText">Rong neu khong doc duoc file (van cho apply — weekly-plan muc rui ro).</param>
public sealed record CvSnapshot(
    Guid CvId,
    Guid CandidateUserId,
    string Name,
    string ExtractedText);

public sealed record ApplicationSnapshot(
    Guid ApplicationId,
    string Status,
    JobSnapshot Job,
    CvSnapshot Cv);
