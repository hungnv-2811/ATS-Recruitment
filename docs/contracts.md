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

public interface IAnonymizer
{
    AnonymizedCv Anonymize(string rawCvText);
}
```

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

public interface IScreeningQueue
{
    Task EnqueueAsync(ScreeningTask task, CancellationToken ct);
    Task<ScreeningTask?> DequeueAsync(CancellationToken ct);
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
```

### 2.4. Cross-module port (Recruitment → AiScreening)

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
                              int? AiScore, string? AiSummary);
```

### AI

```csharp
public record ScorePreviewRequest(Guid CvId, Guid JobId);
public record ScorePreviewResponse(int Score, string Summary,
                                    IReadOnlyList<string> Strengths,
                                    IReadOnlyList<string> Gaps);

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
POST   /api/ai/score-preview              (candidate: xem % phù hợp trước khi nộp)
POST   /api/screening-jobs                (HR: khởi chạy sàng lọc lô)
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
