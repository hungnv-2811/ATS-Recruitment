# ATS.Recruitment.Domain

Entity nghiệp vụ tuyển dụng, không có bất kỳ phụ thuộc hạ tầng nào.

## Nội dung dự kiến

- **Entities**: `Job`, `Candidate`, `HrProfile`, `Cv`, `Application`, `Interview`, `Evaluation`
- **Value Objects**: `ApplicationStatus`, `JobStatus`
- **Ports** (`Ports/`):
  - `IJobRepository`, `ICandidateRepository`, `ICvRepository`
  - `IApplicationRepository`, `IInterviewRepository`
  - `IFileStorage`

**KHÔNG có ở đây:** `IAnonymizer` và `AnonymizedCv`. Hai thứ đó thuộc `AiScreening.Domain`.
Đặt ở đây thì module này phải tham chiếu kiểu của module khác (vi phạm ranh giới module), và
constructor `internal` của `AnonymizedCv` sẽ không biên dịch được qua ranh giới assembly.
Xem `docs/architecture.md` ADR-3.

Xem `docs/contracts.md`.
