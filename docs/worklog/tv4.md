# Worklog — TV4 — AI & Chất lượng

| | |
|---|---|
| **Họ tên** | Tiền |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | AI & Chất lượng |
| **Project sở hữu** | `ATS.AiScreening.*`, `src/Tests/` |

> Mẫu ghi: [_TEMPLATE.md](_TEMPLATE.md). Tuần mới nhất đặt **trên cùng**.

> **Lưu ý khi nộp:** các ô `#—` (Issue/PR) và `~—h` (thời gian) là phần **mỗi người tự điền**
> theo hoạt động thật của mình trên GitHub. Phần "Việc đã làm" ghi đúng trạng thái repo.

---

## Tuần 5–9 (dd/mm – dd/mm) — Module AiScreening end-to-end (nhánh `feature/aiscreening-pipeline`)

**Chức năng chung của tuần:** pipeline AI chạy được từ API/Worker tới DB với FakeAdapter, adapter
thật (OpenAI, Embedding) đứng sau fallback, hạn mức + cache hoạt động, toàn bộ endpoint AI theo
`contracts.md` mục 8.3.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Domain: `AiScore`, `AiScoreCacheEntry`, `ScreeningJob`, `AiUsageQuota`, `InterviewQuestionEntry` + port `IAiScoreRepository`, `IScreeningJobRepository`, `IInterviewQuestionRepository` | #— | #— | Xong |
| `SimpleAnonymizer`: mở rộng regex PII (SĐT nhiều dạng, link MXH, ngày sinh, địa chỉ, trường, họ tên) + trích `Skills`/`ExperienceKeywords` (`SkillCatalog`) | #— | #— | Xong |
| Application: `PreviewScoreHandler` (cache trước → trừ lượt), `StartScreeningHandler`, `ProcessScreeningHandler`, sinh/lưu câu hỏi, truy vấn điểm/lô/hạn mức, `JobRequirementParser` | #— | #— | Xong |
| Infrastructure: Fake/Keyword/OpenAI/Embedding adapter, `AiScoringPipeline` (ngân sách 30s), câu hỏi OpenAI + Template, Redis/InMemory queue, EF repo + quota `ON CONFLICT`, adapter cross-module | #— | #— | Xong |
| 9 endpoint AI (429 kèm `previewRemainingToday` + `resetsAt`) | #— | #— | Xong |
| Test: 123 unit (`ATS.AiScreening.Tests`), +5 quy tắc kiến trúc (14 test), 19 integration test endpoint AI | #— | #— | Xong |
| Cập nhật `ai-integration.md` mục 3 + mục 9 (chỗ lệch thiết kế) | #— | #— | Xong |

### Vướng mắc

- `dotnet test` bị Smart App Control chặn trên Windows → chạy CI trong WSL (`ci-wsl.sh`, bản sao `tv4`).
- Test integration dùng chung DB InMemory trong một fixture: cache theo NỘI DUNG làm hai test có
  cùng CV+JD trúng cache của nhau → dữ liệu test phải khác nội dung (đúng thiết kế, không phải bug).
- Đường Postgres (`ON CONFLICT`, `ExecuteUpdate` tiến độ lô, đếm `hit_count`) chưa chạy trên DB thật —
  cần test ở pha tích hợp khi có migration.

### Kế hoạch tuần sau

- Pha tích hợp: test end-to-end nộp đơn → Worker chấm (sau khi merge nhánh TV2 + Worker của TV1).
- Đo tỉ lệ sót PII trên 20 CV thật; chạy workflow `ai-smoke` với khoá OpenAI trước buổi bảo vệ.

**Thời gian bỏ ra:** ~—h


---

## Tuần 2 (dd/mm – dd/mm) — Dựng khung

**Chức năng chung của tuần:** `docker compose up` chạy được, ArchitectureTests xanh.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ATS.ArchitectureTests`: 5 quy tắc / 9 test, reflection thuần (bỏ NetArchTest) | #— | #— | Xong |
| Kiểm chứng test bằng cách **cố tình phá luật** → đúng 2 test đỏ, 7 xanh | #— | #— | Xong |
| `AnonymizedCv` (ctor `internal`) + `IAnonymizer` + `SimpleAnonymizer` trong Domain | #— | #— | Xong |
| `IPiiRedactor` — đường đi cho anonymizer LLM ở phase 2 mà không phá ADR-3 | #— | #— | Xong |
| Port AI: `IAiScoringService`, `IInterviewQuestionGenerator`, `IAiScoreCacheRepository`, `IAiUsageQuota` | #— | #— | Xong |
| 13 unit test cho anonymizer, `CacheKey`, `Score` | #— | #— | Xong |

### Vướng mắc

- Quy tắc 3 (Controller không đụng `DbContext`) hiện còn **rỗng** vì tuần 2 chưa có controller
  nào. Phải rà lại ở tuần 3 khi `ATS.Api` có controller đầu tiên, nếu không nó pass mà không
  kiểm gì cả.

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

-

### Kế hoạch tuần sau

-

**Thời gian bỏ ra:** ~—h

---

## Tuần 1 (dd/mm – dd/mm) — Khởi động

**Chức năng chung của tuần:** phân tích yêu cầu, chốt phạm vi & kiến trúc, khởi tạo repo.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Nghiên cứu pipeline AI, đăng ký API key | #— | #— | Chưa bắt đầu |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

-

### Kế hoạch tuần sau

-

**Thời gian bỏ ra:** ~—h

---
