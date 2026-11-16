# Worklog — TV2 — Nghiệp vụ & API

| | |
|---|---|
| **Họ tên** | Tuấn Anh |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | Nghiệp vụ & API |
| **Project sở hữu** | `ATS.Recruitment.*`, `ATS.Identity.*`, `ATS.Api` |

> Mẫu ghi: [_TEMPLATE.md](_TEMPLATE.md). Tuần mới nhất đặt **trên cùng**.

> **Lưu ý khi nộp:** các ô `#—` (Issue/PR) và `~—h` (thời gian) là phần **mỗi người tự điền**
> theo hoạt động thật của mình trên GitHub. Phần "Việc đã làm" ghi đúng trạng thái repo.


---

## Tuần 3–5, 8 (dd/mm – dd/mm) — Identity + Recruitment (nhánh `feature/recruitment-identity-core`)

**Chức năng chung của tuần:** đăng ký/đăng nhập 2 vai + quên mật khẩu (tuần 3); HR đăng tin,
ứng viên quản lý nhiều CV (tuần 4); nộp đơn + danh sách xếp hạng (tuần 5); hẹn phỏng vấn,
email mời, đánh giá (tuần 8).

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Identity.Domain: `User`, `PasswordResetToken` (hash SHA-256, 30 phút, dùng 1 lần), 4 port | #— | #— | Xong |
| `AuthService` (register/login/forgot/reset/me) + `UserAdminService` (list, khoá/mở, đổi vai) | #— | #— | Xong |
| `BCryptPasswordHasher`, `JwtTokenIssuer` (claim `sub`/`email`/`name`/`role`), seed Admin đầu tiên | #— | #— | Xong |
| Đăng ký tạo luôn hồ sơ `candidates`/`hr_profiles` qua port `IUserProfileProvisioner` (cùng một `SaveChanges`) | #— | #— | Xong |
| Recruitment.Domain: `Job` (Draft/Open/Closed), `Candidate`, `HrProfile`, `Cv`, `Application` (state machine), `Interview`, `Evaluation` | #— | #— | Xong |
| `JobService`, `CvService` (pdf/docx ≤ 5 MB, trích text đồng bộ, CV đầu tiên mặc định), `ApplicationService` (RB9), `InterviewService` (mail ngoài transaction) | #— | #— | Xong |
| EF config + repository cho 9 bảng; `IRecruitmentQueries` cho AiScreening | #— | #— | Xong |
| Endpoint `/api/auth`, `/api/me`, `/api/admin/users`, `/api/jobs`, `/api/hr/*`, `/api/me/cvs`, `/api/applications`, `/api/interviews` | #— | #— | Xong |
| 95 test trong `ATS.Recruitment.Tests` (EF InMemory + fake viết tay; kiểm DDL Npgsql sinh từ model) | #— | #— | Xong |
| Ghi lệch contract vào `contracts.md` 8.4, cập nhật UC-01/02/04 | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- Windows Smart App Control chặn nạp DLL test → chạy build/test trong WSL (`ci-wsl.sh`).
- Lớp `Application` (entity) trùng tên namespace `ATS.Recruitment.Application` → dùng alias `JobApplication` ở tầng Application/Infrastructure.
- Quy ước snake_case đổi tên index có tên → phải `HasDatabaseName("ix_cvs_default")` tường minh (test DDL bắt được).

### Kế hoạch tuần sau

- Ghép nhánh với AiScreening (adapter thật của `IApplicationScreeningTrigger`/`IApplicationScoreReader`) và kiểm tra end-to-end trên Postgres sau khi TV1 sinh migration.
- Test tích hợp HTTP cho các endpoint (cùng TV4).

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
