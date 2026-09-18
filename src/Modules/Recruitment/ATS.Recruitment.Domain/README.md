# ATS.Recruitment.Domain

Entity nghiệp vụ tuyển dụng, không có bất kỳ phụ thuộc hạ tầng nào.

## Nội dung dự kiến

- **Entities**: `Job`, `Candidate`, `Cv`, `Application`, `Interview`, `Evaluation`
- **Value Objects**: `ApplicationStatus`, `JobStatus`
- **Ports** (`Ports/`):
  - `IJobRepository`, `ICandidateRepository`, `ICvRepository`
  - `IApplicationRepository`, `IInterviewRepository`
  - `IFileStorage`, `IAnonymizer`

Xem `docs/contracts.md`.
