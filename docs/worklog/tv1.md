# Worklog — TV1 — Dữ liệu & Hạ tầng

| | |
|---|---|
| **Họ tên** | Hoạt (nhóm trưởng) |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | Dữ liệu & Hạ tầng |
| **Project sở hữu** | `ATS.SharedKernel`, `ATS.Persistence`, `docker/`, `.github/` |

> **Cách đọc:** mỗi tuần một mục, tuần mới nhất ở trên. Việc được xếp vào tuần theo
> [`weekly-plan.md`](../weekly-plan.md); tuần 1 bắt đầu 07/09/2026. Các ô `#—` (Issue/PR) và
> `~—h` (thời gian) mỗi người **tự điền theo hoạt động thật** của mình trên GitHub.

---

## Tuần 10 (09/11 – 15/11) — Tích hợp, kiểm thử trên hệ thống thật, hoàn thiện

**Chức năng chung của tuần:** Merge đủ các nhánh, migration + seed, chạy `docker compose` end-to-end, sửa lỗi phát hiện.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Migration `AddModuleTables` (14 bảng), FK xuyên schema DEFERRABLE, FK kép CV thuộc đúng ứng viên | #— | #— | Xong |
| `DatabaseSeeder`: dữ liệu demo idempotent (3 HR, 8 ứng viên, 10 tin, 30 đơn), ghi CV PDF thật | #— | #— | Xong |
| Kiểm trên Postgres 16 thật: migrate, seed 2 lần, dashboard, quota, lô sàng lọc qua Redis + Worker; sửa Worker thiếu `AddIdentityModule` | #— | #— | Xong |

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

## Tuần 9 (02/11 – 08/11) — Tuần đệm — Embedding, báo cáo, phân quyền UI

**Chức năng chung của tuần:** Trả nợ tuần 7–8, thêm Embedding + dashboard, chặn route theo vai, hoàn thiện giao diện.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Dashboard `GET /api/reports/dashboard`: SQL xuyên 3 schema, HR xem tin của mình, Admin toàn hệ thống | #— | #— | Xong |
| Test hạ tầng dùng chung + dashboard; `database-design.md` cập nhật quy ước cột/enum | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- Chưa có entity của TV2/TV4 trong nhánh → SQL dashboard kiểm trên Postgres ở tuần 10.

### Kế hoạch tuần sau

- Migration `AddModuleTables` (14 bảng), FK xuyên schema DEFERRABLE, FK kép CV thuộc đúng ứng viên

**Thời gian bỏ ra:** ~—h

---

## Tuần 8 (26/10 – 01/11) — Phỏng vấn + gợi ý câu hỏi + email

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

## Tuần 7 (19/10 – 25/10) — Adapter AI thật — nhiều tầng, cache, hạn mức

**Chức năng chung của tuần:** Pipeline thật có fallback; xem độ phù hợp lần 2 trúng cache, không trừ lượt.

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

## Tuần 6 (12/10 – 18/10) — AiScreening với FakeAdapter (end-to-end)

**Chức năng chung của tuần:** Nộp đơn → hàng đợi → Worker → điểm lưu DB, dùng Fake để không tốn tiền.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ScreeningWorker`: lấy việc từ hàng đợi Redis, mỗi việc một scope, một việc lỗi không giết worker, dừng gọn | #— | #— | Xong |

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

## Tuần 5 (05/10 – 11/10) — Ứng tuyển + ẩn danh CV

**Chức năng chung của tuần:** Ứng viên nộp CV vào tin và thấy trong lịch sử; CV được ẩn danh trước khi tới AI.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Khung `ATS.Worker`: Composition Root dùng chung danh sách module với API | #— | #— | Xong |
| Review EF config của TV2 (bảng/cột snake_case, enum lưu chuỗi) | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `ScreeningWorker`: lấy việc từ hàng đợi Redis, mỗi việc một scope, một việc lỗi không giết worker, dừng gọn

**Thời gian bỏ ra:** ~—h

---

## Tuần 4 (28/09 – 04/10) — Recruitment — tin tuyển dụng, nhiều CV

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

- Khung `ATS.Worker`: Composition Root dùng chung danh sách module với API

**Thời gian bỏ ra:** ~—h

---

## Tuần 3 (21/09 – 27/09) — Identity — đăng ký/đăng nhập 2 vai, quên mật khẩu

**Chức năng chung của tuần:** Đăng ký ứng viên → đăng nhập → nhận JWT → gọi `/api/me`; quên mật khẩu qua MailHog.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `SmtpEmailSender` (MailHog/SMTP thật, lỗi ném cho người gọi xử lý) + `NullEmailSender`, chọn theo `Smtp:Host` | #— | #— | Xong |
| Test chọn adapter email theo cấu hình | #— | #— | Xong |

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

## Tuần 2 (14/09 – 20/09) — Dựng khung

**Chức năng chung của tuần:** Clone repo, `docker compose up` thấy API xanh, ArchitectureTests pass.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ATS.sln` 18 project, project reference đúng chiều phụ thuộc; `Directory.Build.props`, `Directory.Packages.props`, `global.json` | #— | #— | Xong |
| `ATS.SharedKernel` (`Entity<TId>`, `Result<T>`, `Error`, `IEmailSender`) + `ATS.Persistence` (`AtsDbContext`) + migration `InitialCreate` | #— | #— | Xong |
| `docker-compose.yml` 6 service + Dockerfile .NET 10; CI build + test | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `SmtpEmailSender` (MailHog/SMTP thật, lỗi ném cho người gọi xử lý) + `NullEmailSender`, chọn theo `Smtp:Host`

**Thời gian bỏ ra:** ~—h

---

## Tuần 1 (07/09 – 13/09) — Khảo sát, chốt phạm vi & thiết kế

**Chức năng chung của tuần:** Khảo sát hệ thống tương tự, người dùng, công nghệ; tài liệu thiết kế đóng, cả nhóm thống nhất ranh giới module và API.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Khảo sát hệ thống tương tự (TopCV, ITviec, VietnamWorks, LinkedIn, ATS) — `khao-sat.md` mục 1, cùng TV2 | #— | #— | Xong |
| Khảo sát công nghệ: kiến trúc, CSDL, hàng đợi, triển khai — `khao-sat.md` mục 3, cùng TV4 | #— | #— | Xong |
| `architecture.md`: 3 ADR, ràng buộc RB1–RB9, đánh đổi; `database-design.md`: ERD, schema, ràng buộc, index | #— | #— | Xong |
| Tạo repo, CODEOWNERS; review chéo phát hiện `candidates` xếp nhầm schema `identity` | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `ATS.sln` 18 project, project reference đúng chiều phụ thuộc; `Directory.Build.props`, `Directory.Packages.props`, `global.json`

**Thời gian bỏ ra:** ~—h

---
