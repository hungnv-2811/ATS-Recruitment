namespace ATS.Web.Models;

// DTO phia Web — chep theo docs/contracts.md muc 8.3. Web KHONG tham chieu project module
// nao (README muc 3), nen ban sao nay la co y: API doi ten truong thi UI phai doi theo,
// va do la ly do muc 8 ghi "doi bat ky dong nao = bao ca nhom".
// Trang thai/nguon diem de dang string vi API tra enum duoi dang chuoi.

// ---- Identity ----
public sealed record RegisterRequest(string Email, string Password, string FullName, string Role);

public sealed record LoginRequest(string Email, string Password);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    string FullName,
    string Role);

public sealed record UserDto(Guid Id, string Email, string? FullName, string Role, bool IsActive, DateTimeOffset CreatedAt);

public sealed record UpdateUserRequest(bool? IsActive, string? Role);

// ---- CV ----
public sealed record CvDto(
    Guid Id,
    string Name,
    string FileType,
    int FileSizeKb,
    bool IsDefault,
    DateTimeOffset UploadedAt,
    bool HasText);

// ---- Job ----
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

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed record CreateJobRequest(
    string Title,
    string Description,
    string Requirements,
    string Location,
    string EmploymentType,
    bool Publish);

public sealed record UpdateJobRequest(
    string? Title,
    string? Description,
    string? Requirements,
    string? Location,
    string? EmploymentType);

// ---- Ung tuyen ----
public sealed record ApplyRequest(Guid JobId, Guid CvId);

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
    string? ScreeningStatus);

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
    string? ScreeningStatus);

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
    DateTimeOffset? UpdatedAt,
    int? AiScore,
    string? AiSummary,
    string? AiAdapterUsed,
    string? ScreeningStatus);

public sealed record ChangeStatusRequest(string Status, string? Note);

// ---- Phong van ----
public sealed record ScheduleInterviewRequest(
    DateTimeOffset ScheduledAt,
    string Mode,
    string LocationOrLink,
    string InterviewerName);

public sealed record EvaluationDto(Guid Id, int Rating, string Comment, string Recommendation, DateTimeOffset CreatedAt);

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

public sealed record SubmitEvaluationRequest(int Rating, string Comment, string Recommendation);

// ---- AI ----
public sealed record ScorePreviewRequest(Guid CvId, Guid JobId);

public sealed record ScorePreviewResponse(
    int Score,
    string Summary,
    IReadOnlyList<string>? Strengths,
    IReadOnlyList<string>? Gaps,
    string AdapterUsed,
    bool CacheHit,
    int PreviewRemainingToday);

public sealed record QuotaDto(
    int PreviewUsedToday,
    int PreviewLimitPerDay,
    int PreviewRemainingToday,
    int BatchUsedToday,
    int BatchLimitPerDay,
    DateTimeOffset? ResetsAt);

public sealed record StartScreeningRequest(Guid JobId);

public sealed record ScreeningJobDto(
    Guid Id,
    Guid JobId,
    int TargetCount,
    int DoneCount,
    int FailedCount,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt)
{
    public bool IsFinished => Status is "Done" or "Cancelled";
}

public sealed record AiScoreDto(
    Guid ApplicationId,
    int? Score,
    string? Summary,
    IReadOnlyList<string>? Strengths,
    IReadOnlyList<string>? Gaps,
    string? AdapterUsed,
    string? ScreeningStatus,
    DateTimeOffset? ScoredAt);

public sealed record InterviewQuestionDto(string Category, string Question, string Source);

public sealed record SaveQuestionsRequest(IReadOnlyList<InterviewQuestionDto> Questions);

// ---- Bao cao ----
public sealed record TopJobDto(Guid JobId, string Title, int ApplicationCount);

public sealed record AiStatsDto(
    int ScoredCount,
    int FailedCount,
    int CacheEntries,
    int CacheHits,
    double CacheHitRate,
    IReadOnlyDictionary<string, int>? ByAdapter);

public sealed record DashboardDto(
    int TotalJobs,
    int OpenJobs,
    int TotalApplications,
    IReadOnlyDictionary<string, int>? ApplicationsByStatus,
    double HiredRate,
    IReadOnlyList<TopJobDto>? TopJobs,
    AiStatsDto? Ai);
