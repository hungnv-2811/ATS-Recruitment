# Worklog — TV4 — AI & Chất lượng

| | |
|---|---|
| **Họ tên** | Tiền |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | AI & Chất lượng |
| **Project sở hữu** | `ATS.AiScreening.*`, `src/Tests/` |

> **Cách đọc:** mỗi tuần một mục, tuần mới nhất ở trên. Việc được xếp vào tuần theo
> [`weekly-plan.md`](../weekly-plan.md). Các ô `dd/mm`, `#—` (Issue/PR) và `~—h` (thời gian)
> mỗi người **tự điền theo hoạt động thật** của mình trên GitHub.

---

## Tuần 10 (dd/mm – dd/mm) — Tích hợp, kiểm thử trên hệ thống thật, hoàn thiện

**Chức năng chung của tuần:** Merge đủ các nhánh, migration + seed, chạy `docker compose` end-to-end, sửa lỗi phát hiện.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| 9 test end-to-end qua HTTP: vòng đời nộp đơn → chấm → sàng lọc lô → phỏng vấn → Hired, phân quyền chéo, RB9 | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- Hàng đợi in-memory dùng chung trong một test class → rút sạch trước khi đếm.

### Kế hoạch tuần sau

- Bảo vệ đồ án.

**Thời gian bỏ ra:** ~—h

---

## Tuần 9 (dd/mm – dd/mm) — Tuần đệm — Embedding, báo cáo, phân quyền UI

**Chức năng chung của tuần:** Trả nợ tuần 7–8, thêm Embedding + dashboard, chặn route theo vai.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `EmbeddingScoringAdapter` chèn giữa pipeline | #— | #— | Xong |
| Thêm 5 quy tắc kiến trúc (14 test); 19 integration test endpoint AI; cập nhật `ai-integration.md` | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- 9 test end-to-end qua HTTP: vòng đời nộp đơn → chấm → sàng lọc lô → phỏng vấn → Hired, phân quyền chéo, RB9

**Thời gian bỏ ra:** ~—h

---

## Tuần 8 (dd/mm – dd/mm) — Phỏng vấn + gợi ý câu hỏi + email

**Chức năng chung của tuần:** HR hẹn phỏng vấn, AI gợi ý câu hỏi, MailHog nhận thư; SMTP lỗi vẫn lưu buổi phỏng vấn.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `OpenAiInterviewQuestionAdapter` + `TemplateInterviewQuestionAdapter` (fallback 10 câu) | #— | #— | Xong |
| Sinh/lưu/đọc câu hỏi phỏng vấn + endpoint | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `EmbeddingScoringAdapter` chèn giữa pipeline

**Thời gian bỏ ra:** ~—h

---

## Tuần 7 (dd/mm – dd/mm) — Adapter AI thật — chấm điểm nhiều tầng, cache, hạn mức

**Chức năng chung của tuần:** Pipeline thật có fallback; preview lần 2 trúng cache, không trừ lượt.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `OpenAiScoringAdapter` (JSON strict, retry), `KeywordScoringAdapter`, `AiScoringPipeline` (ngân sách 30s) | #— | #— | Xong |
| Cache theo nội dung + hạn mức `ON CONFLICT`; `PreviewScoreHandler` (tra cache trước, trúng cache không trừ lượt) | #— | #— | Xong |
| Endpoint `score-preview` (429 kèm `previewRemainingToday`, `resetsAt`), `ai-quota`; snapshot test 5 cặp CV–JD | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- Đường Postgres (`ON CONFLICT`, tiến độ lô) chỉ kiểm được khi có migration (tuần 10).

### Kế hoạch tuần sau

- `OpenAiInterviewQuestionAdapter` + `TemplateInterviewQuestionAdapter` (fallback 10 câu)

**Thời gian bỏ ra:** ~—h

---

## Tuần 6 (dd/mm – dd/mm) — AiScreening với FakeAdapter (end-to-end)

**Chức năng chung của tuần:** Nộp đơn → hàng đợi → Worker → điểm lưu DB, dùng Fake để không tốn tiền.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Entity `AiScore`, `ScreeningJob`, `AiUsageQuota`, `AiScoreCacheEntry` + repository | #— | #— | Xong |
| `FakeAiScoringAdapter`, hàng đợi Redis (+ InMemory khi chưa cấu hình), `StartScreeningHandler`, `ProcessScreeningHandler` | #— | #— | Xong |
| Adapter liên module: `ScreeningTriggerAdapter`, `ApplicationScoreReader`; endpoint sàng lọc lô | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `OpenAiScoringAdapter` (JSON strict, retry), `KeywordScoringAdapter`, `AiScoringPipeline` (ngân sách 30s)

**Thời gian bỏ ra:** ~—h

---

## Tuần 5 (dd/mm – dd/mm) — Ứng tuyển + ẩn danh CV

**Chức năng chung của tuần:** Ứng viên nộp CV vào tin và thấy trong lịch sử; CV được ẩn danh trước khi tới AI.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `SimpleAnonymizer`: mở rộng regex PII (SĐT nhiều dạng, link MXH, ngày sinh, địa chỉ, trường, họ tên) | #— | #— | Xong |
| Trích `Skills`/`ExperienceKeywords` (`SkillCatalog`) + unit test PII | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Entity `AiScore`, `ScreeningJob`, `AiUsageQuota`, `AiScoreCacheEntry` + repository

**Thời gian bỏ ra:** ~—h

---

## Tuần 4 (dd/mm – dd/mm) — Recruitment — tin tuyển dụng, nhiều CV

**Chức năng chung của tuần:** Ứng viên upload nhiều CV, HR đăng tin, cả hai xem được.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Review PR Recruitment của TV2 | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `SimpleAnonymizer`: mở rộng regex PII (SĐT nhiều dạng, link MXH, ngày sinh, địa chỉ, trường, họ tên)

**Thời gian bỏ ra:** ~—h

---

## Tuần 3 (dd/mm – dd/mm) — Identity — đăng ký/đăng nhập 2 vai, quên mật khẩu

**Chức năng chung của tuần:** Đăng ký ứng viên → đăng nhập → nhận JWT → gọi `/api/me`; quên mật khẩu qua MailHog.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Review PR Identity của TV2 (theo kế hoạch tuần 3–4: hỗ trợ review, viết test cho module khác) | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Review PR Recruitment của TV2

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
