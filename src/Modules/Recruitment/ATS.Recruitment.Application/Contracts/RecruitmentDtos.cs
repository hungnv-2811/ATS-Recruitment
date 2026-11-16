namespace ATS.Recruitment.Application.Contracts;

// DTO cua API Recruitment — TEN TRUONG dung docs/contracts.md muc 8.3 (UI cua TV3 dung theo).
// Moi trang thai/enum la CHUOI ("Open", "Applied", "Scheduled", "Hire"...) de JSON ra chuoi ma
// khong phai cau hinh JsonStringEnumConverter o Program.cs.

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

// ---- Job ------------------------------------------------------------------------------

public sealed record JobDto(
    Guid Id,
    string Title,
    string Description,
    string Requirements,
    string Location,
    string EmploymentType,
    string Status,
    DateTimeOffset? PostedAt,
    DateTimeOffset? ClosedAt,
    int ApplicationCount);

/// <param name="Publish">true = dang ngay (Open), false = luu nhap (Draft).</param>
public sealed record CreateJobRequest(
    string Title,
    string Description,
    string? Requirements,
    string? Location,
    string EmploymentType,
    bool Publish);

/// <summary>Truong null = giu nguyen.</summary>
public sealed record UpdateJobRequest(
    string? Title,
    string? Description,
    string? Requirements,
    string? Location,
    string? EmploymentType);

public sealed record JobSearchQuery(
    string? Search,
    string? Location,
    string? EmploymentType,
    int Page = 1,
    int PageSize = 20);

// ---- CV -------------------------------------------------------------------------------

/// <param name="HasText">false khi khong trich duoc chu (AI se khong cham duoc CV nay).</param>
public sealed record CvDto(
    Guid Id,
    string Name,
    string FileType,
    int FileSizeKb,
    bool IsDefault,
    DateTimeOffset UploadedAt,
    bool HasText);

/// <summary>Du lieu upload da tach khoi IFormFile (Application khong biet ASP.NET Core).</summary>
public sealed record UploadCvCommand(string? Name, string? FileName, long Length, Stream Content);

/// <summary>File goc de tai ve.</summary>
public sealed record CvFile(Stream Content, string ContentType, string FileName);

// ---- Application ----------------------------------------------------------------------

public sealed record ApplyRequest(Guid JobId, Guid CvId);

public sealed record ChangeStatusRequest(string Status, string? Note);

public sealed record ApplicationDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    Guid CvId,
    string CvName,
    string Status,
    DateTimeOffset AppliedAt,
    int? AiScore,
    string? AiSummary,
    string? AiAdapterUsed,
    string ScreeningStatus);

public sealed record RankedApplicationDto(
    Guid Id,
    string CandidateName,
    Guid CvId,
    string CvName,
    string Status,
    DateTimeOffset AppliedAt,
    int? AiScore,
    string? AiSummary,
    string? AiAdapterUsed,
    string ScreeningStatus);

/// <param name="HrNote">Chi HR chu tin thay; ung vien nhan null.</param>
public sealed record ApplicationDetailDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string CandidateName,
    string? CandidateEmail,
    Guid CvId,
    string CvName,
    string Status,
    string? HrNote,
    DateTimeOffset AppliedAt,
    DateTimeOffset UpdatedAt,
    int? AiScore,
    string? AiSummary,
    string? AiAdapterUsed,
    string ScreeningStatus);

// ---- Interview ------------------------------------------------------------------------

/// <param name="Mode">"online" | "onsite".</param>
public sealed record ScheduleInterviewRequest(
    DateTimeOffset ScheduledAt,
    string Mode,
    string LocationOrLink,
    string InterviewerName);

public sealed record InterviewDto(
    Guid Id,
    Guid ApplicationId,
    DateTimeOffset ScheduledAt,
    string Mode,
    string LocationOrLink,
    string InterviewerName,
    string Status,
    bool EmailSent,
    EvaluationDto? Evaluation);

public sealed record InterviewListItemDto(
    Guid Id,
    Guid ApplicationId,
    string JobTitle,
    string CandidateName,
    DateTimeOffset ScheduledAt,
    string Mode,
    string Status,
    bool EmailSent);

/// <param name="Recommendation">"Hire" | "Reject" | "Consider".</param>
public sealed record SubmitEvaluationRequest(int Rating, string? Comment, string Recommendation);

public sealed record EvaluationDto(Guid Id, int Rating, string? Comment, string Recommendation, DateTimeOffset CreatedAt);
