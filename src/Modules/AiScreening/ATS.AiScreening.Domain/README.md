# ATS.AiScreening.Domain

⭐ Lõi AI — nơi thể hiện Ports & Adapters mạnh nhất.

## Nội dung dự kiến

- **Entities**: `AiScore`, `ScreeningJob`, `InterviewQuestion`
- **Value Objects**: `AnonymizedCv`, `CacheKey`, `Score`, `ScreeningResult`, `JobRequirement`
- **Ports** (`Ports/`):
  - `IAiScoringService` — chấm phù hợp CV ↔ JD
  - `IInterviewQuestionGenerator` — sinh câu hỏi phỏng vấn
  - `IAnonymizer` — ẩn danh CV (**port VÀ bản hiện thực đều ở module này**)
  - `IPiiRedactor` — port cấp thấp cho anonymizer bằng LLM ở phase 2
  - `IScreeningQueue` — hàng đợi bất đồng bộ
  - `IAiScoreRepository`, `IAiScoreCacheRepository`, `IScreeningJobRepository`, `IAiUsageQuota`
- **Domain service**: `SimpleAnonymizer` (regex thuần, không I/O)

**Ràng buộc 1:** cả 2 port AI **chỉ nhận `AnonymizedCv`**, không nhận `string` hay `Cv` thô.
ArchitectureTests ép quy tắc này.

**Ràng buộc 2:** constructor của `AnonymizedCv` là `internal`. `SimpleAnonymizer` nằm **trong
chính assembly này** nên tạo được, và **không assembly nào khác** tạo được — kể cả
`AiScreening.Infrastructure`. Tuyệt đối không thêm `InternalsVisibleTo`: làm vậy là vứt bỏ
bảo đảm compile-time mà cả đồ án dựa vào. Anonymizer bằng LLM phải đi qua `IPiiRedactor`
(chỉ nhận/trả `string`). Xem `docs/architecture.md` ADR-3.
