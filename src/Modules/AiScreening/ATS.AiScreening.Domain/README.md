# ATS.AiScreening.Domain

⭐ Lõi AI — nơi thể hiện Ports & Adapters mạnh nhất.

## Nội dung dự kiến

- **Entities**: `AiScore`, `ScreeningJob`, `InterviewQuestion`
- **Value Objects**: `AnonymizedCv`, `CacheKey`, `Score`, `ScreeningResult`, `JobRequirement`
- **Ports** (`Ports/`):
  - `IAiScoringService` — chấm phù hợp CV ↔ JD
  - `IInterviewQuestionGenerator` — sinh câu hỏi phỏng vấn
  - `IScreeningQueue` — hàng đợi bất đồng bộ
  - `IAiScoreRepository`, `IScreeningJobRepository`

**Ràng buộc:** cả 2 port AI **chỉ nhận `AnonymizedCv`**, không nhận `string` hay `Cv` thô.
ArchitectureTests ép quy tắc này.
