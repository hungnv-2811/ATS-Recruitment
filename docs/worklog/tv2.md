# Worklog — TV2 — Nghiệp vụ & API

| | |
|---|---|
| **Họ tên** | Tuấn Anh |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | Nghiệp vụ & API |
| **Project sở hữu** | `ATS.Recruitment.*`, `ATS.Identity.*`, `ATS.Api` |

> **Cách đọc:** mỗi tuần một mục, tuần mới nhất ở trên. Việc được xếp vào tuần theo
> [`weekly-plan.md`](../weekly-plan.md). Các ô `dd/mm`, `#—` (Issue/PR) và `~—h` (thời gian)
> mỗi người **tự điền theo hoạt động thật** của mình trên GitHub.


---

## Tuần 10 (dd/mm – dd/mm) — Tích hợp, kiểm thử trên hệ thống thật, hoàn thiện

**Chức năng chung của tuần:** Merge đủ các nhánh, migration + seed, chạy `docker compose` end-to-end, sửa lỗi phát hiện.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Test sinh DDL Postgres từ model (`EfModelTests`) — bắt lỗi tên index `ix_cvs_default` | #— | #— | Xong |
| Ghi các chỗ lệch contract vào `contracts.md` 8.4, cập nhật UC-01/02/04 | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Bảo vệ đồ án.

**Thời gian bỏ ra:** ~—h

---

## Tuần 9 (dd/mm – dd/mm) — Tuần đệm — Embedding, báo cáo, phân quyền UI

**Chức năng chung của tuần:** Trả nợ tuần 7–8, thêm Embedding + dashboard, chặn route theo vai.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `UserAdminService` + `/api/admin/users`: khoá/mở, đổi vai, chặn Admin tự khoá mình | #— | #— | Xong |
| Hỗ trợ TV3 nối UI ↔ API (đối chiếu DTO với contracts mục 8.3) | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Test sinh DDL Postgres từ model (`EfModelTests`) — bắt lỗi tên index `ix_cvs_default`

**Thời gian bỏ ra:** ~—h

---

## Tuần 8 (dd/mm – dd/mm) — Phỏng vấn + gợi ý câu hỏi + email

**Chức năng chung của tuần:** HR hẹn phỏng vấn, AI gợi ý câu hỏi, MailHog nhận thư; SMTP lỗi vẫn lưu buổi phỏng vấn.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `Interview`, `Evaluation` + `InterviewService`: lưu trước, gửi mail sau và ngoài transaction, SMTP lỗi → `EmailSent=false` | #— | #— | Xong |
| Gửi lại lời mời, nhập đánh giá; endpoint `/api/applications/{id}/interviews`, `/api/interviews/*`, `/api/hr/interviews` + test | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `UserAdminService` + `/api/admin/users`: khoá/mở, đổi vai, chặn Admin tự khoá mình

**Thời gian bỏ ra:** ~—h

---

## Tuần 7 (dd/mm – dd/mm) — Adapter AI thật — chấm điểm nhiều tầng, cache, hạn mức

**Chức năng chung của tuần:** Pipeline thật có fallback; preview lần 2 trúng cache, không trừ lượt.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Hỗ trợ TV4 tích hợp adapter AI vào luồng nộp đơn (review contract `IApplicationScreeningTrigger`) | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `Interview`, `Evaluation` + `InterviewService`: lưu trước, gửi mail sau và ngoài transaction, SMTP lỗi → `EmailSent=false`

**Thời gian bỏ ra:** ~—h

---

## Tuần 6 (dd/mm – dd/mm) — AiScreening với FakeAdapter (end-to-end)

**Chức năng chung của tuần:** Nộp đơn → hàng đợi → Worker → điểm lưu DB, dùng Fake để không tốn tiền.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Danh sách xếp hạng cho HR ghép điểm qua `IApplicationScoreReader` (điểm giảm dần, chưa chấm ở cuối) | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Hỗ trợ TV4 tích hợp adapter AI vào luồng nộp đơn (review contract `IApplicationScreeningTrigger`)

**Thời gian bỏ ra:** ~—h

---

## Tuần 5 (dd/mm – dd/mm) — Ứng tuyển + ẩn danh CV

**Chức năng chung của tuần:** Ứng viên nộp CV vào tin và thấy trong lịch sử; CV được ẩn danh trước khi tới AI.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `Application` (state machine, không nhảy cóc) + `ApplicationService` (lỗi trigger AI không làm hỏng nộp đơn — RB9) | #— | #— | Xong |
| `IRecruitmentQueries` cho module AiScreening đọc CV/JD | #— | #— | Xong |
| Endpoint `/api/applications`, `/api/me/applications` + test | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- Entity `Application` trùng tên namespace → dùng alias `JobApplication`.

### Kế hoạch tuần sau

- Danh sách xếp hạng cho HR ghép điểm qua `IApplicationScoreReader` (điểm giảm dần, chưa chấm ở cuối)

**Thời gian bỏ ra:** ~—h

---

## Tuần 4 (dd/mm – dd/mm) — Recruitment — tin tuyển dụng, nhiều CV

**Chức năng chung của tuần:** Ứng viên upload nhiều CV, HR đăng tin, cả hai xem được.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Recruitment.Domain: `Job` (Draft/Open/Closed), `Candidate`, `HrProfile`, `Cv` (1-N, một CV mặc định) | #— | #— | Xong |
| `JobService`, `CvService` (pdf/docx ≤ 5 MB, trích text đồng bộ, CV đầu tiên tự mặc định, xoá CV đã nộp → 409) | #— | #— | Xong |
| EF config + repository; endpoint `/api/jobs`, `/api/hr/jobs`, `/api/me/cvs` + test | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- Quy ước snake_case đổi tên index → phải đặt `HasDatabaseName` tường minh.

### Kế hoạch tuần sau

- `Application` (state machine, không nhảy cóc) + `ApplicationService` (lỗi trigger AI không làm hỏng nộp đơn — RB9)

**Thời gian bỏ ra:** ~—h

---

## Tuần 3 (dd/mm – dd/mm) — Identity — đăng ký/đăng nhập 2 vai, quên mật khẩu

**Chức năng chung của tuần:** Đăng ký ứng viên → đăng nhập → nhận JWT → gọi `/api/me`; quên mật khẩu qua MailHog.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Identity.Domain: `User`, `PasswordResetToken` (lưu hash SHA-256, hết hạn 30 phút, dùng 1 lần) + 4 port | #— | #— | Xong |
| `AuthService`: đăng ký 2 vai, đăng nhập, quên mật khẩu (luôn 204), đặt lại mật khẩu, `/api/me` | #— | #— | Xong |
| `BCryptPasswordHasher`, `JwtTokenIssuer` (claim `sub`/`email`/`name`/`role`), tạo Admin đầu tiên | #— | #— | Xong |
| Đăng ký tạo luôn hồ sơ `candidates`/`hr_profiles` qua port `IUserProfileProvisioner` | #— | #— | Xong |
| Endpoint `/api/auth/*`, `/api/me` + test AuthService, token hết hạn, token dùng lần 2 | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Recruitment.Domain: `Job` (Draft/Open/Closed), `Candidate`, `HrProfile`, `Cv` (1-N, một CV mặc định)

**Thời gian bỏ ra:** ~—h

---

## Tuần 2 (dd/mm – dd/mm) — Dựng khung

**Chức năng chung của tuần:** `docker compose up` chạy được, ArchitectureTests xanh.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ATS.Api` skeleton: Composition Root, Swagger, `GET /health` | #— | #— | Xong |
| JWT middleware — cấu hình sẵn, chưa endpoint nào `[Authorize]` (bật thật tuần 3) | #— | #— | Xong |
| Log cảnh báo `AiProvider` lúc khởi động (chặn rủi ro api/worker lệch cấu hình) | #— | #— | Xong |
| `IApplicationScreeningTrigger` — port cross-module đặt ở bên chủ động gọi | #— | #— | Xong |

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
| Phân tích yêu cầu, vẽ use case tổng quát | #— | #— | Chưa bắt đầu |

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
