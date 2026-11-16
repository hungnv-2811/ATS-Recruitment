# Worklog — TV4 — AI & Chất lượng

| | |
|---|---|
| **Họ tên** | Tiền |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | AI & Chất lượng |
| **Project sở hữu** | `ATS.AiScreening.*`, `src/Tests/` |

> **Cách đọc:** mỗi tuần một mục, tuần mới nhất ở trên. Việc được xếp vào tuần theo
> [`weekly-plan.md`](../weekly-plan.md); tuần 1 bắt đầu 07/09/2026. Các ô `#—` (Issue/PR) và
> `~—h` (thời gian) mỗi người **tự điền theo hoạt động thật** của mình trên GitHub.

---

## Tuần 10 (09/11 – 15/11) — Tích hợp, kiểm thử trên hệ thống thật, hoàn thiện

**Chức năng chung của tuần:** Merge đủ các nhánh, migration + seed, chạy `docker compose` end-to-end, sửa lỗi phát hiện.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| 9 test end-to-end qua HTTP: nộp đơn → chấm → sàng lọc lô → phỏng vấn → Hired, phân quyền chéo, RB9 | #— | #— | Xong |
| Review bộ test Postgres thật (quota, unique, seeder) và factory test bỏ qua kiểm phiên | #— | #— | Đang làm |

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

## Tuần 9 (02/11 – 08/11) — Tuần đệm — Embedding, báo cáo, phân quyền UI

**Chức năng chung của tuần:** Trả nợ tuần 7–8, thêm Embedding + dashboard, chặn route theo vai, hoàn thiện giao diện.

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

- 9 test end-to-end qua HTTP: nộp đơn → chấm → sàng lọc lô → phỏng vấn → Hired, phân quyền chéo, RB9

**Thời gian bỏ ra:** ~—h

---

## Tuần 8 (26/10 – 01/11) — Phỏng vấn + gợi ý câu hỏi + email

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

## Tuần 7 (19/10 – 25/10) — Adapter AI thật — nhiều tầng, cache, hạn mức

**Chức năng chung của tuần:** Pipeline thật có fallback; xem độ phù hợp lần 2 trúng cache, không trừ lượt.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `OpenAiScoringAdapter` (JSON strict, retry), `KeywordScoringAdapter`, `AiScoringPipeline` (ngân sách 30s) | #— | #— | Xong |
| Cache theo nội dung + hạn mức `ON CONFLICT`; endpoint `score-preview` (429), `ai-quota`; snapshot test 5 cặp CV–JD | #— | #— | Xong |

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

## Tuần 6 (12/10 – 18/10) — AiScreening với FakeAdapter (end-to-end)

**Chức năng chung của tuần:** Nộp đơn → hàng đợi → Worker → điểm lưu DB, dùng Fake để không tốn tiền.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `FakeAiScoringAdapter`, hàng đợi Redis (+ InMemory khi chưa cấu hình), `StartScreeningHandler`, `ProcessScreeningHandler` | #— | #— | Xong |
| Adapter liên module (`ScreeningTriggerAdapter`, `ApplicationScoreReader`); endpoint sàng lọc lô | #— | #— | Xong |

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

## Tuần 5 (05/10 – 11/10) — Ứng tuyển + ẩn danh CV

**Chức năng chung của tuần:** Ứng viên nộp CV vào tin và thấy trong lịch sử; CV được ẩn danh trước khi tới AI.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Entity `AiScore`, `ScreeningJob`, `AiUsageQuota`, `AiScoreCacheEntry`, câu hỏi phỏng vấn + EF config, repository | #— | #— | Xong |
| Quy tắc kiến trúc: `Recruitment` không tham chiếu `AnonymizedCv` | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `FakeAiScoringAdapter`, hàng đợi Redis (+ InMemory khi chưa cấu hình), `StartScreeningHandler`, `ProcessScreeningHandler`

**Thời gian bỏ ra:** ~—h

---

## Tuần 4 (28/09 – 04/10) — Recruitment — tin tuyển dụng, nhiều CV

**Chức năng chung của tuần:** Ứng viên upload nhiều CV, HR đăng tin, cả hai xem được.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `SimpleAnonymizer`: mở rộng regex PII (SĐT nhiều dạng, link MXH, ngày sinh, địa chỉ, trường, họ tên) | #— | #— | Xong |
| Unit test PII + trích `Skills`/`ExperienceKeywords`; review PR Recruitment của TV2 | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Entity `AiScore`, `ScreeningJob`, `AiUsageQuota`, `AiScoreCacheEntry`, câu hỏi phỏng vấn + EF config, repository

**Thời gian bỏ ra:** ~—h

---

## Tuần 3 (21/09 – 27/09) — Identity — đăng ký/đăng nhập 2 vai, quên mật khẩu

**Chức năng chung của tuần:** Đăng ký ứng viên → đăng nhập → nhận JWT → gọi `/api/me`; quên mật khẩu qua MailHog.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `SkillCatalog` + `JobRequirementParser`: tách kỹ năng bắt buộc/ưu tiên và số năm kinh nghiệm từ JD | #— | #— | Xong |
| Review PR Identity của TV2 | #— | #— | Xong |

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

## Tuần 2 (14/09 – 20/09) — Dựng khung

**Chức năng chung của tuần:** Clone repo, `docker compose up` thấy API xanh, ArchitectureTests pass.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ATS.ArchitectureTests`: 5 quy tắc / 9 test, reflection thuần, kiểm chứng bằng cách cố tình phá luật | #— | #— | Xong |
| `AnonymizedCv` (ctor internal), `CacheKey`, `Score` + unit test | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `SkillCatalog` + `JobRequirementParser`: tách kỹ năng bắt buộc/ưu tiên và số năm kinh nghiệm từ JD

**Thời gian bỏ ra:** ~—h

---

## Tuần 1 (07/09 – 13/09) — Khảo sát, chốt phạm vi & thiết kế

**Chức năng chung của tuần:** Khảo sát hệ thống tương tự, người dùng, công nghệ; tài liệu thiết kế đóng, cả nhóm thống nhất ranh giới module và API.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Khảo sát công nghệ AI: so sánh nhà cung cấp LLM, chi phí — `khao-sat.md` mục 3, cùng TV1 | #— | #— | Xong |
| `ai-integration.md`: prompt v1, pipeline fallback, chi phí | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `ATS.ArchitectureTests`: 5 quy tắc / 9 test, reflection thuần, kiểm chứng bằng cách cố tình phá luật

**Thời gian bỏ ra:** ~—h

---
