# Contracts — Interface & DTO

> TV2 phụ trách. Contract-first: interface & DTO chốt trước tuần 3, team làm song song.

## 1. Nguyên tắc

- **Domain định nghĩa Port** (interface). Infrastructure viết Adapter.
- **Application dùng Port qua DI**, không đụng SDK/EF trực tiếp.
- **DTO tách khỏi Entity.** DTO nằm ở Application/Api layer; Entity ở Domain.
- **Value Object cho concept quan trọng.** `AnonymizedCv`, `CacheKey`, `Score` không phải string/int.

## 2. Port trong Domain

### 2.1. Recruitment.Domain

```csharp
namespace ATS.Recruitment.Domain.Ports;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Job>> ListOpenAsync(CancellationToken ct);
    Task AddAsync(Job job, CancellationToken ct);
    Task UpdateAsync(Job job, CancellationToken ct);
}

public interface ICandidateRepository
{
    Task<Candidate?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<Candidate?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Candidate candidate, CancellationToken ct);
}

public interface ICvRepository
{
    Task<Cv?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Cv>> ListByCandidateAsync(Guid candidateId, CancellationToken ct);
    Task AddAsync(Cv cv, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface IApplicationRepository
{
    Task<Application?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Application>> ListByJobAsync(Guid jobId, CancellationToken ct);
    Task<IReadOnlyList<Application>> ListByCandidateAsync(Guid candidateId, CancellationToken ct);
    Task AddAsync(Application application, CancellationToken ct);
    Task UpdateStatusAsync(Guid id, ApplicationStatus status, CancellationToken ct);
}

public interface IInterviewRepository
{
    Task<Interview?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Interview interview, CancellationToken ct);
}

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct);
    Task<Stream> ReadAsync(string path, CancellationToken ct);
    Task DeleteAsync(string path, CancellationToken ct);
}
```

> **`IAnonymizer` KHÔNG nằm ở đây.** Nó thuộc `AiScreening.Domain` (mục 2.2) cùng với
> `AnonymizedCv`. Đặt ở `Recruitment.Domain` thì module này phải tham chiếu kiểu của module
> kia — vi phạm quy tắc "không reference chéo" (`architecture.md` mục 3.3) — và constructor
> `internal` của `AnonymizedCv` sẽ không biên dịch được qua ranh giới assembly. Xem ADR-3.

### 2.2. AiScreening.Domain — hai port AI

```csharp
namespace ATS.AiScreening.Domain.Ports;

public interface IAiScoringService
{
    Task<ScreeningResult> ScoreAsync(
        AnonymizedCv cv,
        JobRequirement jd,
        CancellationToken ct);
}

public interface IInterviewQuestionGenerator
{
    Task<IReadOnlyList<InterviewQuestion>> GenerateAsync(
        AnonymizedCv cv,
        JobRequirement jd,
        CancellationToken ct);
}

// Ẩn danh CV — sống ở ĐÂY, không ở Recruitment (ADR-3).
// SimpleAnonymizer implement port này NGAY TRONG Domain, nhờ đó tạo được
// AnonymizedCv (ctor internal) mà không cần InternalsVisibleTo.
public interface IAnonymizer
{
    Task<AnonymizedCv> AnonymizeAsync(string rawCvText, CancellationToken ct);
}

// Port cấp thấp cho phase 2: adapter Infrastructure chỉ được cầm string,
// KHÔNG bao giờ cầm quyền tạo AnonymizedCv.
public interface IPiiRedactor
{
    Task<string> RedactAsync(string rawCvText, CancellationToken ct);
}

public interface IScreeningQueue
{
    Task EnqueueAsync(ScreeningTask task, CancellationToken ct);
    Task<ScreeningTask?> DequeueAsync(CancellationToken ct);
}

// Cache theo NỘI DUNG, không gắn application_id — dùng được cả khi ứng viên
// preview lúc chưa nộp đơn (UC-05). Xem database-design.md mục 2.
public interface IAiScoreCacheRepository
{
    Task<CachedScore?> GetAsync(CacheKey key, CancellationToken ct);
    Task SaveAsync(CacheKey key, ScreeningResult result, CancellationToken ct);
}

// Hạn mức AI — hiện thực RB2 ($20-30 cả kỳ).
public interface IAiUsageQuota
{
    Task<bool> TryConsumePreviewAsync(Guid userId, CancellationToken ct);
    Task<bool> TryConsumeBatchAsync(Guid userId, CancellationToken ct);
    Task<QuotaStatus> GetStatusAsync(Guid userId, CancellationToken ct);
}

public interface IAiScoreRepository
{
    Task<AiScore?> GetByApplicationAsync(Guid applicationId, CancellationToken ct);
    Task<AiScore?> GetByCacheKeyAsync(string cacheKey, CancellationToken ct);
    Task UpsertAsync(AiScore score, CancellationToken ct);
}

public interface IScreeningJobRepository
{
    Task<ScreeningJob?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(ScreeningJob job, CancellationToken ct);
    Task UpdateProgressAsync(Guid id, int done, int failed, CancellationToken ct);
}
```

### 2.3. Identity.Domain

```csharp
namespace ATS.Identity.Domain.Ports;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}

public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hash);
}

public interface ITokenIssuer
{
    string IssueAccessToken(User user);
}

// "Quên mật khẩu" — token lưu dạng HASH, dùng một lần, hết hạn 30 phút.
public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task AddAsync(PasswordResetToken token, CancellationToken ct);
    Task MarkUsedAsync(Guid id, CancellationToken ct);
    Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct);
}
```

### 2.4. Gửi email — `ATS.SharedKernel`

Hai module cùng cần gửi email (Identity: đặt lại mật khẩu; Recruitment: lời mời phỏng vấn).
Port đặt ở `SharedKernel` vì đây là **hạ tầng kỹ thuật, không mang nghiệp vụ** — đúng ranh
giới mà `architecture.md` mục 3 đặt ra cho SharedKernel. Nếu để ở một module thì module kia
phải reference chéo.

```csharp
namespace ATS.SharedKernel.Ports;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

public sealed record EmailMessage(
    string ToAddress,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);
```

Adapter: `SmtpEmailSender` (MailHog khi dev, SMTP thật khi deploy) và `NullEmailSender` (ghi
log rồi bỏ qua — dùng trong test và khi chưa cấu hình SMTP). Gửi email **không bao giờ** được
làm hỏng nghiệp vụ: nếu SMTP lỗi thì buổi phỏng vấn vẫn phải được lưu.

### 2.5. Cross-module port (Recruitment → AiScreening)

Đặt trong `Recruitment.Application` (vì Recruitment là bên chủ động gọi):

```csharp
namespace ATS.Recruitment.Application.Ports;

public interface IApplicationScreeningTrigger
{
    Task RequestScreeningAsync(Guid applicationId, CancellationToken ct);
}
```

Adapter implement ở `AiScreening.Infrastructure.ScreeningTriggerAdapter` — thin wrapper enqueue vào Redis.

## 3. Value Objects quan trọng

```csharp
// AiScreening.Domain
public sealed record AnonymizedCv
{
    public string Text { get; }
    public IReadOnlyList<string> Skills { get; }
    public IReadOnlyList<string> ExperienceKeywords { get; }

    // Constructor internal — chỉ IAnonymizer được tạo
    internal AnonymizedCv(string text, IEnumerable<string> skills, IEnumerable<string> exp) { ... }
}

public sealed record CacheKey(string Value)
{
    public static CacheKey From(string cvText, string jdText, string modelVersion, string promptVersion)
        => new(Sha256($"{cvText}|{jdText}|{modelVersion}|{promptVersion}"));
}

public sealed record Score
{
    public int Value { get; }
    public Score(int value)
    {
        if (value < 0 || value > 100) throw new ArgumentException(nameof(value));
        Value = value;
    }
}

public sealed record ScreeningResult(
    Score Score,
    string Summary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    string AdapterUsed);

public sealed record InterviewQuestion(
    string Category,   // "technical" | "behavioral" | "situational"
    string Question,
    string Source);    // "AI" | "Manual" | "Template"

public sealed record CachedScore(
    Score Score,
    string Summary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    string AdapterUsed,
    string ModelVersion,
    string PromptVersion,
    DateTime CreatedAt);

public sealed record QuotaStatus(
    int PreviewUsedToday,
    int PreviewLimitPerDay,
    int BatchUsedToday,
    int BatchLimitPerDay);

public sealed record JobRequirement(
    string Title,
    string Description,
    IReadOnlyList<string> RequiredSkills,
    IReadOnlyList<string> NiceToHaveSkills,
    int MinYearsExperience);
```

## 4. DTO cho API

### Đăng ký / đăng nhập

```csharp
public record RegisterRequest(string Email, string Password, string FullName, string Role);
public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, string Role, Guid UserId);

public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
```

### CV

```csharp
public record CvDto(Guid Id, string Name, string FileType, int FileSizeKb,
                    bool IsDefault, DateTime UploadedAt);
public record UploadCvRequest(string Name);  // file gửi qua multipart
```

### Job

```csharp
public record JobDto(Guid Id, string Title, string Description, string Location,
                     string EmploymentType, string Status, DateTime PostedAt);
public record CreateJobRequest(string Title, string Description, string Requirements,
                                string Location, string EmploymentType);
```

### Application

```csharp
public record ApplyRequest(Guid JobId, Guid CvId);
public record ApplicationDto(Guid Id, Guid JobId, string JobTitle, Guid CvId, string CvName,
                              string Status, DateTime AppliedAt,
                              int? AiScore, string? AiSummary,
                              string? AiAdapterUsed,      // "OpenAI" | "Embedding" | "Keyword"
                              string ScreeningStatus);    // Pending|Processing|Scored|Failed
```

### AI

```csharp
public record ScorePreviewRequest(Guid CvId, Guid JobId);
public record ScorePreviewResponse(int Score, string Summary,
                                    IReadOnlyList<string> Strengths,
                                    IReadOnlyList<string> Gaps,
                                    string AdapterUsed,   // tầng nào tạo ra điểm này
                                    bool CacheHit,
                                    int PreviewRemainingToday);

public record StartScreeningRequest(Guid JobId);
public record ScreeningJobDto(Guid Id, int TargetCount, int DoneCount, int FailedCount,
                               string Status);

public record GenerateQuestionsRequest(Guid ApplicationId);
public record InterviewQuestionDto(string Category, string Question, string Source);
```

## 5. REST endpoint (phác thảo)

```
# Auth
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/forgot-password          (luôn trả 204, KHÔNG tiết lộ email có tồn tại hay không)
POST   /api/auth/reset-password           (token + mật khẩu mới)

# CV
GET    /api/me/cvs
POST   /api/me/cvs                        (multipart: file + Name)
DELETE /api/me/cvs/{id}
PATCH  /api/me/cvs/{id}/default

# Job
GET    /api/jobs                          (list, filter, pagination)
GET    /api/jobs/{id}
POST   /api/jobs                          (HR only)
PATCH  /api/jobs/{id}                     (HR only, owner)
POST   /api/jobs/{id}/close               (HR only, owner)

# Application
POST   /api/applications                  (candidate: apply)
GET    /api/me/applications               (candidate: xem hồ sơ mình đã nộp)
GET    /api/jobs/{jobId}/applications     (HR only, owner của job)
PATCH  /api/applications/{id}/status      (HR only)

# AI
POST   /api/ai/score-preview              (candidate: ĐỒNG BỘ, 200 OK, timeout 30s — ngoại lệ
                                           có chủ đích của ADR-2, xem architecture.md)
GET    /api/me/ai-quota                   (candidate: còn bao nhiêu lượt preview hôm nay)
POST   /api/screening-jobs                (HR: khởi chạy sàng lọc lô → 202 Accepted)
GET    /api/screening-jobs/{id}           (polling)
POST   /api/applications/{id}/questions   (HR: sinh câu hỏi phỏng vấn)

# Interview
POST   /api/applications/{id}/interviews  (HR: hẹn phỏng vấn)
POST   /api/interviews/{id}/evaluations   (HR: nhập đánh giá)
```

## 6. Contract giữa các module

| Từ | Đến | Interface | Ai implement |
|---|---|---|---|
| Recruitment.Application | AiScreening | `IApplicationScreeningTrigger` | AiScreening.Infrastructure |
| Recruitment.Application | Hạ tầng email | `IEmailSender` (SharedKernel) | Recruitment.Infrastructure (`SmtpEmailSender`) |
| Identity.Application | Hạ tầng email | `IEmailSender` (SharedKernel) | Recruitment.Infrastructure (`SmtpEmailSender`) |
| ATS.Api | Recruitment.Application | `IJobService`, `ICvService`, `IApplicationService` | Recruitment.Application |
| ATS.Api | AiScreening.Application | `IScoringService`, `IInterviewQuestionService` | AiScreening.Application |
| ATS.Api | Identity.Application | `IAuthService` | Identity.Application |
| ATS.Worker | AiScreening.Application | `IProcessScreeningHandler` | AiScreening.Application |

## 7. Quy ước

- Method async: hậu tố `Async` + `CancellationToken` cuối
- Trả `Task<T?>` khi có thể null (không throw NotFoundException — trả null hoặc `Result<T>`)
- Không dùng `dynamic`, không dùng `object` làm tham số
- DTO là `record` (immutable), Entity là `class` (có behavior)
- Không trả `IQueryable` ra khỏi Repository — trả `IReadOnlyList<T>` hoặc `T`
- Vượt hạn mức AI → `429 Too Many Requests`, body kèm `PreviewRemainingToday` và thời điểm reset
- Mọi response có điểm AI **phải kèm `AdapterUsed`**. Điểm từ `Keyword` và điểm từ `OpenAI` là
  hai thang khác nhau; giấu nguồn đi là để HR so sánh nhầm hai con số không cùng đơn vị

---

## 8. Chốt tuần 3 — contract chi tiết để 4 người làm song song

> Cả nhóm chốt ở buổi sync đầu tuần 3. Mục này **ghi đè** phần "phác thảo" ở mục 4–5 khi hai
> chỗ khác nhau. Đổi bất kỳ dòng nào dưới đây = báo cả nhóm trước (TV3 dựng UI theo đúng mục này).

### 8.1. Quy ước chung

- JSON **camelCase**; enum trả về dạng **chuỗi** (`"Applied"`, `"Open"`…); thời gian là
  `DateTimeOffset` ISO-8601 UTC.
- Lỗi luôn là **ProblemDetails** có thêm trường `code` (`validation` | `not_found` | `conflict` |
  `forbidden` | `unauthorized` | `quota_exceeded` | `external_service`). Ánh xạ status một chỗ:
  `ATS.Api/Http/ResultHttpExtensions.cs`.
- JWT: claim `sub` (user id), `email`, `name` (họ tên), `role` ∈ `Candidate` | `HR` | `Admin`.
  Hạn token: `Jwt:ExpiresMinutes` (mặc định 120). Policy: `CandidateOnly`, `HrOnly`, `AdminOnly`
  (`ATS.Api/Auth/AuthConventions.cs`).
- Bảng/cột **snake_case** tự động (`UseSnakeCaseNamingConvention`); mỗi
  `IEntityTypeConfiguration` chỉ đặt **tên bảng + schema** tường minh, ví dụ
  `builder.ToTable("cvs", AtsDbContext.RecruitmentSchema)`.
- Mỗi module tự đăng ký DI trong `XxxModule.AddXxxModule(...)`; endpoint trong
  `ATS.Api/Endpoints/XxxEndpoints.cs`. `Program.cs` đã nối sẵn, không ai cần sửa nó để thêm tính năng.

### 8.2. Contract liên module (đã có code)

| Interface | Đặt ở | Hiện thực | Ghi chú |
|---|---|---|---|
| `IApplicationScreeningTrigger` | `Recruitment.Application/Ports` | `AiScreening.Infrastructure` | Gọi sau khi nộp đơn thành công |
| `IApplicationScoreReader` | `Recruitment.Application/Ports` | `AiScreening.Infrastructure` | Recruitment ghép điểm vào danh sách xếp hạng |
| `IRecruitmentQueries` | `Recruitment.Application/Contracts` | `Recruitment.Infrastructure` | AiScreening đọc CV/JD qua đây, **không** đọc bảng `recruitment.*` trực tiếp |
| `IProcessScreeningHandler` | `AiScreening.Application/Abstractions` | `AiScreening.Application` | Worker gọi cho mỗi `ScreeningTask` |
| `ICvTextExtractor` | `Recruitment.Domain/Ports` | `Recruitment.Infrastructure/Shared` | Trích text **đồng bộ lúc upload**, lưu `cvs.extracted_text` |

`Recruitment` đăng ký bản NoOp của hai port đầu bằng `TryAdd`; `AiScreening` đăng ký **sau** và
ghi đè bằng adapter thật. Nhờ vậy mỗi nhánh feature tự build/test được.

### 8.3. Endpoint và DTO

**Auth / Identity — TV2**

| Method | Route | Quyền | Request → Response |
|---|---|---|---|
| POST | `/api/auth/register` | ẩn danh | `RegisterRequest(Email, Password, FullName, Role)` — `Role` ∈ `Candidate`/`HR` → `200 AuthResponse`; trùng email `409` |
| POST | `/api/auth/login` | ẩn danh | `LoginRequest(Email, Password)` → `200 AuthResponse`; sai `401` |
| POST | `/api/auth/forgot-password` | ẩn danh | `ForgotPasswordRequest(Email)` → **luôn `204`**. Mail chứa link `{Web:BaseUrl}/reset-password?token=...` |
| POST | `/api/auth/reset-password` | ẩn danh | `ResetPasswordRequest(Token, NewPassword)` → `204`; token sai/hết hạn/đã dùng `400` |
| GET | `/api/me` | đăng nhập | → `MeDto(UserId, Email, FullName, Role)` |
| GET | `/api/admin/users` | Admin | → `UserDto[]` |
| PATCH | `/api/admin/users/{id}` | Admin | `UpdateUserRequest(IsActive?, Role?)` → `204` |

`AuthResponse(AccessToken, ExpiresAt, UserId, Email, FullName, Role)` ·
`UserDto(Id, Email, FullName, Role, IsActive, CreatedAt)` · mật khẩu tối thiểu 8 ký tự.

**CV — TV2**

| Method | Route | Quyền | Request → Response |
|---|---|---|---|
| GET | `/api/me/cvs` | Candidate | → `CvDto[]` |
| POST | `/api/me/cvs` | Candidate | multipart `file` + `name` (pdf/docx, ≤ 5 MB) → `201 CvDto`. CV đầu tiên tự thành mặc định |
| DELETE | `/api/me/cvs/{id}` | Candidate | → `204`; CV đã dùng để nộp đơn → `409` |
| PATCH | `/api/me/cvs/{id}/default` | Candidate | → `204` |

`CvDto(Id, Name, FileType, FileSizeKb, IsDefault, UploadedAt, HasText)` — `HasText=false` khi
không trích được chữ.

**Job — TV2**

| Method | Route | Quyền | Request → Response |
|---|---|---|---|
| GET | `/api/jobs?search=&location=&employmentType=&page=1&pageSize=20` | ẩn danh | → `PagedResult<JobDto>` — chỉ tin `Open` |
| GET | `/api/jobs/{id}` | ẩn danh (tin Open) / HR chủ tin | → `JobDto` |
| GET | `/api/hr/jobs` | HR | → `JobDto[]` của chính HR, mọi trạng thái |
| POST | `/api/jobs` | HR | `CreateJobRequest(Title, Description, Requirements, Location, EmploymentType, Publish)` → `201 JobDto` |
| PATCH | `/api/jobs/{id}` | HR chủ tin | `UpdateJobRequest(Title?, Description?, Requirements?, Location?, EmploymentType?)` → `200 JobDto` |
| POST | `/api/jobs/{id}/publish` | HR chủ tin | → `204` (Draft → Open) |
| POST | `/api/jobs/{id}/close` | HR chủ tin | → `204` |

`JobDto(Id, Title, Description, Requirements, Location, EmploymentType, Status, PostedAt, ClosedAt, ApplicationCount)` ·
`PagedResult<T>(Items, Total, Page, PageSize)` · `EmploymentType` ∈ `FullTime`/`PartTime`/`Internship`/`Contract`.

**Ứng tuyển + phỏng vấn — TV2**

| Method | Route | Quyền | Request → Response |
|---|---|---|---|
| POST | `/api/applications` | Candidate | `ApplyRequest(JobId, CvId)` → `201 ApplicationDto`; đã nộp tin này `409`; tin không Open `400` |
| GET | `/api/me/applications` | Candidate | → `ApplicationDto[]` |
| GET | `/api/jobs/{jobId}/applications?status=` | HR chủ tin | → `RankedApplicationDto[]` xếp điểm giảm dần, **chưa chấm ở cuối** (RB9) |
| GET | `/api/applications/{id}` | HR chủ tin / Candidate chủ đơn | → `ApplicationDetailDto` |
| GET | `/api/applications/{id}/cv-file` | HR chủ tin / Candidate chủ đơn | → file gốc |
| PATCH | `/api/applications/{id}/status` | HR chủ tin | `ChangeStatusRequest(Status, Note?)` → `204`; chuyển sai luật `400` |
| POST | `/api/applications/{id}/interviews` | HR chủ tin | `ScheduleInterviewRequest(ScheduledAt, Mode, LocationOrLink, InterviewerName)` → `201 InterviewDto` |
| GET | `/api/applications/{id}/interviews` | HR chủ tin / Candidate chủ đơn | → `InterviewDto[]` |
| GET | `/api/hr/interviews` | HR | → `InterviewListItemDto[]` (lịch sắp tới của HR) |
| POST | `/api/interviews/{id}/resend-invite` | HR chủ tin | → `204`; SMTP vẫn lỗi `502` |
| POST | `/api/interviews/{id}/evaluations` | HR chủ tin | `SubmitEvaluationRequest(Rating 1–5, Comment, Recommendation)` → `201 EvaluationDto` |

- `ApplicationDto(Id, JobId, JobTitle, CvId, CvName, Status, AppliedAt, AiScore, AiSummary, AiAdapterUsed, ScreeningStatus)`
- `RankedApplicationDto(Id, CandidateName, CvId, CvName, Status, AppliedAt, AiScore, AiSummary, AiAdapterUsed, ScreeningStatus)`
- `ApplicationDetailDto(Id, JobId, JobTitle, CandidateName, CandidateEmail, CvId, CvName, Status, HrNote, AppliedAt, UpdatedAt, AiScore, AiSummary, AiAdapterUsed, ScreeningStatus)`
- `InterviewDto(Id, ApplicationId, ScheduledAt, Mode, LocationOrLink, InterviewerName, Status, EmailSent, Evaluation)` — `Mode` ∈ `online`/`onsite`, `Status` ∈ `Scheduled`/`Done`/`Cancelled`
- `InterviewListItemDto(Id, ApplicationId, JobTitle, CandidateName, ScheduledAt, Mode, Status, EmailSent)`
- `EvaluationDto(Id, Rating, Comment, Recommendation, CreatedAt)` — `Recommendation` ∈ `Hire`/`Reject`/`Consider`
- Luật chuyển trạng thái: `Applied→Screening→Interview→Hired`, từ mọi trạng thái chưa kết thúc
  sang `Rejected`; không nhảy cóc. Hẹn phỏng vấn tự đưa đơn `Applied`/`Screening` sang `Interview`.
- Email mời phỏng vấn gửi **sau** khi lưu, **ngoài** transaction; lỗi SMTP → `EmailSent=false`, vẫn `201`.

**AI — TV4**

| Method | Route | Quyền | Request → Response |
|---|---|---|---|
| POST | `/api/ai/score-preview` | Candidate (CV của mình, tin Open) | `ScorePreviewRequest(CvId, JobId)` → `200 ScorePreviewResponse`; hết lượt `429` (body có `previewRemainingToday`, `resetsAt`) |
| GET | `/api/me/ai-quota` | đăng nhập | → `QuotaDto(PreviewUsedToday, PreviewLimitPerDay, PreviewRemainingToday, BatchUsedToday, BatchLimitPerDay, ResetsAt)` |
| POST | `/api/screening-jobs` | HR chủ tin | `StartScreeningRequest(JobId)` → `202 ScreeningJobDto` |
| GET | `/api/screening-jobs/{id}` | HR | → `ScreeningJobDto` |
| GET | `/api/jobs/{jobId}/screening-jobs/latest` | HR chủ tin | → `ScreeningJobDto` hoặc `404` (UI nối lại thanh tiến độ sau khi rời trang) |
| GET | `/api/applications/{id}/ai-score` | HR chủ tin / Candidate chủ đơn | → `AiScoreDto` |
| POST | `/api/applications/{id}/questions` | HR chủ tin | → `InterviewQuestionDto[]` (sinh, **chưa lưu**) |
| PUT | `/api/interviews/{id}/questions` | HR chủ tin | `SaveQuestionsRequest(Questions)` → `204` |
| GET | `/api/interviews/{id}/questions` | HR chủ tin | → `InterviewQuestionDto[]` |

- `ScorePreviewResponse(Score, Summary, Strengths, Gaps, AdapterUsed, CacheHit, PreviewRemainingToday)`
- `ScreeningJobDto(Id, JobId, TargetCount, DoneCount, FailedCount, Status, StartedAt, FinishedAt)` — `Status` ∈ `Queued`/`Running`/`Done`/`Cancelled`
- `AiScoreDto(ApplicationId, Score, Summary, Strengths, Gaps, AdapterUsed, ScreeningStatus, ScoredAt)`
- `InterviewQuestionDto(Category, Question, Source)`
- `AdapterUsed` ∈ `OpenAI` | `Embedding` | `Keyword` | `Fake`.

**Báo cáo — TV1**

| Method | Route | Quyền | Response |
|---|---|---|---|
| GET | `/api/reports/dashboard` | HR (tin của mình) / Admin (toàn hệ thống) | `DashboardDto` |

`DashboardDto(TotalJobs, OpenJobs, TotalApplications, ApplicationsByStatus, HiredRate, TopJobs, Ai)` —
`ApplicationsByStatus`: map trạng thái → số đơn; `TopJobs`: `[{JobId, Title, ApplicationCount}]` (5 tin);
`Ai`: `{ScoredCount, FailedCount, CacheEntries, CacheHits, CacheHitRate, ByAdapter}`.
