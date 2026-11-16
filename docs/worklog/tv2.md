# Worklog — TV2 — Nghiệp vụ & API

| | |
|---|---|
| **Họ tên** | Tuấn Anh |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | Nghiệp vụ & API |
| **Project sở hữu** | `ATS.Recruitment.*`, `ATS.Identity.*`, `ATS.Api` |

> **Cách đọc:** mỗi tuần một mục, tuần mới nhất ở trên. Việc được xếp vào tuần theo
> [`weekly-plan.md`](../weekly-plan.md); tuần 1 bắt đầu 07/09/2026. Các ô `#—` (Issue/PR) và
> `~—h` (thời gian) mỗi người **tự điền theo hoạt động thật** của mình trên GitHub.

---

## Tuần 10 (09/11 – 15/11) — Tích hợp, kiểm thử trên hệ thống thật, hoàn thiện

**Chức năng chung của tuần:** Merge đủ các nhánh, migration + seed, chạy `docker compose` end-to-end, sửa lỗi phát hiện.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Test sinh DDL Postgres từ model (`EfModelTests`) — bắt lỗi tên index `ix_cvs_default` | #— | #— | Xong |
| Ghi chỗ lệch contract vào `contracts.md` 8.4, cập nhật UC-01/02/04 | #— | #— | Xong |
| Review: nộp đơn trùng đồng thời → 409, thu hồi JWT bằng security stamp, email có dấu (TV3 sửa trong module Identity/Recruitment) | #— | #— | Đang làm |

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

## Tuần 9 (02/11 – 08/11) — Tuần đệm — Embedding, báo cáo, phân quyền UI

**Chức năng chung của tuần:** Trả nợ tuần 7–8, thêm Embedding + dashboard, chặn route theo vai, hoàn thiện giao diện.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `UserAdminService` + `/api/admin/users`: khoá/mở, đổi vai, chặn Admin tự khoá mình | #— | #— | Xong |
| Hỗ trợ TV3 nối UI ↔ API (đối chiếu DTO với `contracts.md` 8.3) | #— | #— | Xong |

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

## Tuần 8 (26/10 – 01/11) — Phỏng vấn + gợi ý câu hỏi + email

**Chức năng chung của tuần:** HR hẹn phỏng vấn, AI gợi ý câu hỏi, MailHog nhận thư; SMTP lỗi vẫn lưu buổi phỏng vấn.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `Interview`, `Evaluation` + `InterviewService`: lưu trước, gửi mail sau và ngoài transaction, SMTP lỗi → `EmailSent=false` | #— | #— | Xong |
| Gửi lại lời mời, nhập đánh giá, lịch phỏng vấn của HR + test | #— | #— | Xong |

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

## Tuần 7 (19/10 – 25/10) — Adapter AI thật — nhiều tầng, cache, hạn mức

**Chức năng chung của tuần:** Pipeline thật có fallback; xem độ phù hợp lần 2 trúng cache, không trừ lượt.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Test `ApplicationService`/state machine: nộp trùng, CV người khác, tin đã đóng, chuyển trạng thái sai luật | #— | #— | Xong |
| Hỗ trợ TV4 tích hợp adapter AI vào luồng nộp đơn (review `IApplicationScreeningTrigger`) | #— | #— | Xong |

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

## Tuần 6 (12/10 – 18/10) — AiScreening với FakeAdapter (end-to-end)

**Chức năng chung của tuần:** Nộp đơn → hàng đợi → Worker → điểm lưu DB, dùng Fake để không tốn tiền.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Danh sách xếp hạng cho HR ghép điểm qua `IApplicationScoreReader` (điểm giảm dần, chưa chấm ở cuối) | #— | #— | Xong |
| Chi tiết hồ sơ + tải CV gốc cho HR/ứng viên chủ đơn | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Test `ApplicationService`/state machine: nộp trùng, CV người khác, tin đã đóng, chuyển trạng thái sai luật

**Thời gian bỏ ra:** ~—h

---

## Tuần 5 (05/10 – 11/10) — Ứng tuyển + ẩn danh CV

**Chức năng chung của tuần:** Ứng viên nộp CV vào tin và thấy trong lịch sử; CV được ẩn danh trước khi tới AI.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `Application` (state machine, không nhảy cóc) + `ApplicationService` (lỗi trigger AI không làm hỏng nộp đơn — RB9) | #— | #— | Xong |
| `IRecruitmentQueries` cho module AiScreening đọc CV/JD; endpoint `/api/applications`, `/api/me/applications` | #— | #— | Xong |

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

## Tuần 4 (28/09 – 04/10) — Recruitment — tin tuyển dụng, nhiều CV

**Chức năng chung của tuần:** Ứng viên upload nhiều CV, HR đăng tin, cả hai xem được.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Đăng ký tạo luôn hồ sơ `candidates`/`hr_profiles` qua port `IUserProfileProvisioner` | #— | #— | Xong |
| Recruitment.Domain: `Job` (Draft/Open/Closed), `Candidate`, `HrProfile`, `Cv` (1-N, một CV mặc định) | #— | #— | Xong |
| `JobService`, `CvService` (pdf/docx ≤ 5 MB, CV đầu tiên tự mặc định); endpoint `/api/jobs`, `/api/hr/jobs`, `/api/me/cvs` | #— | #— | Xong |

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

## Tuần 3 (21/09 – 27/09) — Identity — đăng ký/đăng nhập 2 vai, quên mật khẩu

**Chức năng chung của tuần:** Đăng ký ứng viên → đăng nhập → nhận JWT → gọi `/api/me`; quên mật khẩu qua MailHog.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Identity.Domain: `User`, `PasswordResetToken` (lưu hash SHA-256, hết hạn 30 phút, dùng 1 lần) + 4 port | #— | #— | Xong |
| `AuthService`: đăng ký 2 vai, đăng nhập, quên mật khẩu (luôn 204), đặt lại mật khẩu; `BCryptPasswordHasher`, `JwtTokenIssuer` | #— | #— | Xong |
| Endpoint `/api/auth/*`, `/api/me` + test: trùng email, sai mật khẩu, token hết hạn, token dùng lần 2 | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Đăng ký tạo luôn hồ sơ `candidates`/`hr_profiles` qua port `IUserProfileProvisioner`

**Thời gian bỏ ra:** ~—h

---

## Tuần 2 (14/09 – 20/09) — Dựng khung

**Chức năng chung của tuần:** Clone repo, `docker compose up` thấy API xanh, ArchitectureTests pass.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ATS.Api` skeleton: Swagger, cấu hình JWT, `GET /health` | #— | #— | Xong |
| Cảnh báo cấu hình `AiProvider` lệch giữa api và worker lúc khởi động | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Identity.Domain: `User`, `PasswordResetToken` (lưu hash SHA-256, hết hạn 30 phút, dùng 1 lần) + 4 port

**Thời gian bỏ ra:** ~—h

---

## Tuần 1 (07/09 – 13/09) — Khảo sát, chốt phạm vi & thiết kế

**Chức năng chung của tuần:** Khảo sát hệ thống tương tự, người dùng, công nghệ; tài liệu thiết kế đóng, cả nhóm thống nhất ranh giới module và API.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Khảo sát hệ thống tương tự — `khao-sat.md` mục 1, cùng TV1 | #— | #— | Xong |
| Phiếu khảo sát HR (H1–H10) + phỏng vấn sâu HR — `khao-sat.md` mục 2.3 | #— | #— | Đang làm |
| `use-cases.md` (tác nhân, use case, trạng thái) + `contracts.md` (port, DTO) | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- Chưa phát phiếu khảo sát HR — cần nhờ người quen ở công ty thực tập.

### Kế hoạch tuần sau

- `ATS.Api` skeleton: Swagger, cấu hình JWT, `GET /health`

**Thời gian bỏ ra:** ~—h

---
