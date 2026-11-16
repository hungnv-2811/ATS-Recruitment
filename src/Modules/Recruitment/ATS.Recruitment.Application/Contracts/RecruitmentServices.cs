using ATS.SharedKernel;

namespace ATS.Recruitment.Application.Contracts;

// Cac service ATS.Api goi. Tham so userId luon la claim "sub" cua nguoi dang goi; service tu
// kiem quyen chu so huu (HR chu tin, ung vien chu CV/don) — RB8 ep o day, khong o UI.

public interface IJobService
{
    /// <summary>Danh sach cong khai: chi tin Open.</summary>
    Task<Result<PagedResult<JobDto>>> SearchAsync(JobSearchQuery query, CancellationToken ct = default);

    /// <summary>Tin Open: ai cung xem duoc. Draft/Closed: chi HR chu tin, nguoi khac nhan NotFound.</summary>
    Task<Result<JobDto>> GetAsync(Guid jobId, Guid? callerUserId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<JobDto>>> ListMineAsync(Guid hrUserId, CancellationToken ct = default);

    Task<Result<JobDto>> CreateAsync(Guid hrUserId, CreateJobRequest request, CancellationToken ct = default);

    Task<Result<JobDto>> UpdateAsync(Guid hrUserId, Guid jobId, UpdateJobRequest request, CancellationToken ct = default);

    Task<Result> PublishAsync(Guid hrUserId, Guid jobId, CancellationToken ct = default);

    Task<Result> CloseAsync(Guid hrUserId, Guid jobId, CancellationToken ct = default);
}

public interface ICvService
{
    Task<Result<IReadOnlyList<CvDto>>> ListAsync(Guid candidateUserId, CancellationToken ct = default);

    /// <summary>pdf/docx, toi da 5 MB; trich text dong bo; CV dau tien tu thanh mac dinh.</summary>
    Task<Result<CvDto>> UploadAsync(Guid candidateUserId, UploadCvCommand command, CancellationToken ct = default);

    /// <summary>CV da dung de nop don → Conflict.</summary>
    Task<Result> DeleteAsync(Guid candidateUserId, Guid cvId, CancellationToken ct = default);

    Task<Result> SetDefaultAsync(Guid candidateUserId, Guid cvId, CancellationToken ct = default);
}

public interface IApplicationService
{
    Task<Result<ApplicationDto>> ApplyAsync(Guid candidateUserId, ApplyRequest request, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ApplicationDto>>> ListMineAsync(Guid candidateUserId, CancellationToken ct = default);

    /// <summary>Xep diem AI giam dan, don chua cham o cuoi (RB9).</summary>
    Task<Result<IReadOnlyList<RankedApplicationDto>>> ListForJobAsync(Guid hrUserId, Guid jobId, string? status, CancellationToken ct = default);

    Task<Result<ApplicationDetailDto>> GetDetailAsync(Guid callerUserId, Guid applicationId, CancellationToken ct = default);

    Task<Result<CvFile>> GetCvFileAsync(Guid callerUserId, Guid applicationId, CancellationToken ct = default);

    Task<Result> ChangeStatusAsync(Guid hrUserId, Guid applicationId, ChangeStatusRequest request, CancellationToken ct = default);
}

public interface IInterviewService
{
    /// <summary>Luu truoc, gui mail moi SAU va NGOAI transaction; SMTP loi → EmailSent=false, van thanh cong.</summary>
    Task<Result<InterviewDto>> ScheduleAsync(Guid hrUserId, Guid applicationId, ScheduleInterviewRequest request, CancellationToken ct = default);

    Task<Result<IReadOnlyList<InterviewDto>>> ListByApplicationAsync(Guid callerUserId, Guid applicationId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<InterviewListItemDto>>> ListUpcomingForHrAsync(Guid hrUserId, CancellationToken ct = default);

    /// <summary>SMTP van loi → External (502).</summary>
    Task<Result> ResendInviteAsync(Guid hrUserId, Guid interviewId, CancellationToken ct = default);

    /// <summary>Nhap danh gia → buoi phong van chuyen Done.</summary>
    Task<Result<EvaluationDto>> SubmitEvaluationAsync(Guid hrUserId, Guid interviewId, SubmitEvaluationRequest request, CancellationToken ct = default);
}
