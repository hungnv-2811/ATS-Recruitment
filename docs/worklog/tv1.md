# Worklog — TV1 — Dữ liệu & Hạ tầng

| | |
|---|---|
| **Họ tên** | Hoạt (nhóm trưởng) |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | Dữ liệu & Hạ tầng |
| **Project sở hữu** | `ATS.SharedKernel`, `ATS.Persistence`, `docker/`, `.github/` |

> **Cách đọc:** mỗi tuần một mục, tuần mới nhất ở trên. Việc được xếp vào tuần theo
> [`weekly-plan.md`](../weekly-plan.md). Các ô `dd/mm`, `#—` (Issue/PR) và `~—h` (thời gian)
> mỗi người **tự điền theo hoạt động thật** của mình trên GitHub.


---

## Tuần 10 (dd/mm – dd/mm) — Tích hợp, kiểm thử trên hệ thống thật, hoàn thiện

**Chức năng chung của tuần:** Merge đủ các nhánh, migration + seed, chạy `docker compose` end-to-end, sửa lỗi phát hiện.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Migration duy nhất `AddModuleTables` (14 bảng), FK xuyên schema DEFERRABLE, FK kép CV thuộc đúng ứng viên | #— | #— | Xong |
| `DatabaseSeeder`: dữ liệu demo idempotent (3 HR, 8 ứng viên, 10 tin, 30 đơn), ghi CV PDF thật | #— | #— | Xong |
| Kiểm trên Postgres 16 thật: migrate, seed 2 lần, dashboard, tải CV, quota `ON CONFLICT`, lô sàng lọc qua Redis + Worker | #— | #— | Xong |
| Sửa lỗi Worker chết khi khởi động sau merge (thiếu `AddIdentityModule`) | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- `dotnet ef` trên Windows bị Smart App Control chặn → sinh migration trong WSL.

### Kế hoạch tuần sau

- Bảo vệ đồ án.

**Thời gian bỏ ra:** ~—h

---

## Tuần 9 (dd/mm – dd/mm) — Tuần đệm — Embedding, báo cáo, phân quyền UI

**Chức năng chung của tuần:** Trả nợ tuần 7–8, thêm Embedding + dashboard, chặn route theo vai.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Dashboard `GET /api/reports/dashboard`: SQL xuyên 3 schema, HR xem tin của mình, Admin toàn hệ thống | #— | #— | Xong |
| Test hạ tầng dùng chung + dashboard trong `ATS.IntegrationTests/Shared/` | #— | #— | Xong |
| `database-design.md`: `users.full_name`, quy ước cột/enum, quy trình migration + seed | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- Chưa có entity của TV2/TV4 trong nhánh → SQL dashboard chỉ kiểm được ở tuần 10.

### Kế hoạch tuần sau

- Migration duy nhất `AddModuleTables` (14 bảng), FK xuyên schema DEFERRABLE, FK kép CV thuộc đúng ứng viên

**Thời gian bỏ ra:** ~—h

---

## Tuần 8 (dd/mm – dd/mm) — Phỏng vấn + gợi ý câu hỏi + email

**Chức năng chung của tuần:** HR hẹn phỏng vấn, AI gợi ý câu hỏi, MailHog nhận thư; SMTP lỗi vẫn lưu buổi phỏng vấn.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Compose: thêm `Web__BaseUrl` (link trong email), `Seed__Admin*`, `Database__SeedDemoData` | #— | #— | Xong |
| CI: thêm bước `dotnet format --verify-no-changes` | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Dashboard `GET /api/reports/dashboard`: SQL xuyên 3 schema, HR xem tin của mình, Admin toàn hệ thống

**Thời gian bỏ ra:** ~—h

---

## Tuần 7 (dd/mm – dd/mm) — Adapter AI thật — chấm điểm nhiều tầng, cache, hạn mức

**Chức năng chung của tuần:** Pipeline thật có fallback; preview lần 2 trúng cache, không trừ lượt.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Compose: dồn cấu hình AI vào `x-app-common` để api và worker không thể lệch `AiProvider`; thêm hạn mức AI vào `.env.example` | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Compose: thêm `Web__BaseUrl` (link trong email), `Seed__Admin*`, `Database__SeedDemoData`

**Thời gian bỏ ra:** ~—h

---

## Tuần 6 (dd/mm – dd/mm) — AiScreening với FakeAdapter (end-to-end)

**Chức năng chung của tuần:** Nộp đơn → hàng đợi → Worker → điểm lưu DB, dùng Fake để không tốn tiền.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ATS.Worker`: Composition Root giống API, `ScreeningWorker` mở scope cho mỗi việc, một việc lỗi không giết worker | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Compose: dồn cấu hình AI vào `x-app-common` để api và worker không thể lệch `AiProvider`; thêm hạn mức AI vào `.env.example`

**Thời gian bỏ ra:** ~—h

---

## Tuần 5 (dd/mm – dd/mm) — Ứng tuyển + ẩn danh CV

**Chức năng chung của tuần:** Ứng viên nộp CV vào tin và thấy trong lịch sử; CV được ẩn danh trước khi tới AI.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Hỗ trợ review EF config của TV2 (quy ước bảng/cột snake_case, enum lưu chuỗi) | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `ATS.Worker`: Composition Root giống API, `ScreeningWorker` mở scope cho mỗi việc, một việc lỗi không giết worker

**Thời gian bỏ ra:** ~—h

---

## Tuần 4 (dd/mm – dd/mm) — Recruitment — tin tuyển dụng, nhiều CV

**Chức năng chung của tuần:** Ứng viên upload nhiều CV, HR đăng tin, cả hai xem được.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `LocalFileStorage`: tên file do hệ thống sinh, chặn path traversal khi đọc/xoá | #— | #— | Xong |
| Trích text CV: `PdfPigTextExtractor`, `OpenXmlTextExtractor`, `CompositeCvTextExtractor` (file hỏng → chuỗi rỗng, vẫn cho nộp) | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Hỗ trợ review EF config của TV2 (quy ước bảng/cột snake_case, enum lưu chuỗi)

**Thời gian bỏ ra:** ~—h

---

## Tuần 3 (dd/mm – dd/mm) — Identity — đăng ký/đăng nhập 2 vai, quên mật khẩu

**Chức năng chung của tuần:** Đăng ký ứng viên → đăng nhập → nhận JWT → gọi `/api/me`; quên mật khẩu qua MailHog.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `SmtpEmailSender` (MailHog/SMTP thật, lỗi ném cho người gọi xử lý) + `NullEmailSender`, chọn theo `Smtp:Host` | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `LocalFileStorage`: tên file do hệ thống sinh, chặn path traversal khi đọc/xoá

**Thời gian bỏ ra:** ~—h

---

## Tuần 2 (dd/mm – dd/mm) — Dựng khung

**Chức năng chung của tuần:** `docker compose up` chạy được, ArchitectureTests xanh.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ATS.sln` + 18 project, project reference đúng chiều phụ thuộc | #— | #— | Xong |
| `ATS.SharedKernel`: `Entity<TId>`, `Result<T>`, `Error`, `IEmailSender` | #— | #— | Xong |
| `ATS.Persistence`: `AtsDbContext` + `AddAtsPersistence(...)` nhận danh sách module | #— | #— | Xong |
| Migration `InitialCreate` — tạo 3 schema | #— | #— | Xong |
| `Directory.Build.props` (TargetFramework một chỗ) + `dotnet-tools.json` (ghim EF) | #— | #— | Xong |
| `docker-compose.yml` 6 service + Dockerfile .NET 10 | #— | #— | Xong |
| CI xanh (build + test) | #— | #— | Xong |

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
| Tạo repo, branch protection, CODEOWNERS, board backlog | #— | #— | Xong |
| `architecture.md`: 3 ADR + ràng buộc RB1–RB9 + đánh đổi | #— | #— | Xong |
| `database-design.md`: ERD, schema, ràng buộc, index | #— | #— | Xong |
| Review chéo: phát hiện `candidates` bị xếp nhầm schema `identity` | #— | #— | Xong |

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
