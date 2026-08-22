# ATS-Recruitment — Hệ thống Quản lý Tuyển dụng & Sàng lọc Ứng viên tích hợp AI

Đồ án môn **Chuyên đề tổng hợp (607708)** — Nhóm 4.

Hệ thống ATS (Applicant Tracking System) giúp bộ phận nhân sự quản lý tin tuyển dụng, hồ sơ
ứng viên và quy trình phỏng vấn; đồng thời tích hợp **trợ lý AI** để **tóm tắt CV** và **chấm
độ phù hợp CV–JD**, hỗ trợ HR sàng lọc hàng trăm hồ sơ nhanh và khách quan hơn.

---

## 1. Chức năng

| # | Nhóm chức năng | Mô tả |
|---|---|---|
| 1 | Tài khoản & phân quyền | Đăng ký/đăng nhập, phân quyền Quản trị / HR / Nhà tuyển dụng |
| 2 | Quản lý tin tuyển dụng (Job/JD) | Tạo, sửa, đóng tin; mô tả công việc và yêu cầu |
| 3 | Quản lý ứng viên & CV | Hồ sơ ứng viên, tải lên CV, lưu trữ, tìm kiếm |
| 4 | Quy trình tuyển dụng | Ứng tuyển, vòng phỏng vấn, chuyển trạng thái, lịch hẹn |
| 5 | Báo cáo & thống kê | Dashboard số liệu tuyển dụng, xuất báo cáo |
| **AI** | **Trợ lý sàng lọc** | **Tóm tắt CV + chấm điểm phù hợp CV–JD (0–100) + xếp hạng ứng viên kèm lý do** |

### Chức năng AI hoạt động thế nào

```
CV (PDF)  ──►  1. Trích xuất text (PdfPig)
               2. Ẩn danh dữ liệu nhạy cảm (CCCD, SĐT, email)
               3. Rút trường có cấu trúc (kỹ năng, kinh nghiệm, học vấn)
               4. Sinh vector embedding
JD (text) ──►  5. Cosine similarity CV × JD  ──►  điểm 0–100
               6. LLM sinh tóm tắt CV + lý do phù hợp
               7. Xếp hạng danh sách ứng viên
```

**Khi AI sai:** HR luôn nhìn thấy CV gốc, điểm AI chỉ dùng để tham khảo và sắp xếp — quyết định
cuối cùng do con người. Mỗi kết quả có nút phản hồi để HR hiệu chỉnh, và mọi lần gọi AI đều
được ghi log. Khi dịch vụ AI lỗi, hệ thống vẫn chạy đầy đủ, chỉ mất phần gợi ý xếp hạng.

---

## 2. Kiến trúc

```
┌──────────────────────────────────────────┐
│  ATS.Web        — Blazor (giao diện)     │
├──────────────────────────────────────────┤
│  ATS.Api        — ASP.NET Core Web API   │
│  ATS.Business   — Service / nghiệp vụ    │
├──────────────────────────────────────────┤
│  ATS.Data       — EF Core + Repository   │
├──────────────────────────────────────────┤
│  SQL Server / PostgreSQL                 │
└──────────────────────────────────────────┘
                    ↕ HTTP
              ┌───────────────┐
              │   ATS.AI      │  embedding + LLM
              └───────────────┘

  ATS.Contracts — DTO + interface dùng chung giữa các tầng
  ATS.Tests     — xUnit
```

Chi tiết: [docs/architecture.md](docs/architecture.md)

---

## 3. Công nghệ

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core Web API (.NET 8), C# |
| CSDL | Entity Framework Core + SQL Server |
| Giao diện | Blazor |
| AI | OpenAI/Azure OpenAI — `text-embedding-3` + `gpt-4o-mini` |
| DevOps | Docker, docker-compose, GitHub Actions |
| Kiểm thử | xUnit, Moq |

---

## 4. Cấu trúc thư mục

```
ATS-Recruitment/
├── README.md
├── CONTRIBUTING.md              ← quy ước branch / commit / PR
├── .github/
│   ├── workflows/ci.yml         ← CI: build + test
│   ├── pull_request_template.md
│   └── ISSUE_TEMPLATE/task.md
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
├── docs/
│   ├── architecture.md          ← sơ đồ kiến trúc nhiều lớp
│   ├── database-design.md       ← ERD, mô tả bảng
│   ├── use-cases.md             ← use case tổng quát
│   ├── contracts.md             ← DTO + interface giữa các tầng
│   ├── ai-integration.md        ← pipeline AI, prompt, chi phí, fallback
│   ├── team-assignment.md       ← phân công theo tầng
│   ├── weekly-plan.md           ← kế hoạch 10 tuần
│   └── worklog/                 ← nhật ký công việc từng thành viên
└── src/
    ├── ATS.Contracts/           [cả nhóm — PR cần 2 approve]
    ├── ATS.Data/                [TV1]
    ├── ATS.Business/            [TV2]
    ├── ATS.Api/                 [TV2]
    ├── ATS.Web/                 [TV3]
    ├── ATS.AI/                  [TV4]
    └── ATS.Tests/               [TV4]
```

---

## 5. Cách chạy

### Yêu cầu

- .NET SDK 8.0
- Docker Desktop (nếu chạy bằng container)
- SQL Server (hoặc dùng container trong `docker-compose.yml`)

### Chạy khi phát triển

```bash
dotnet restore
dotnet ef database update --project src/ATS.Data --startup-project src/ATS.Api
dotnet run --project src/ATS.Api      # API   → https://localhost:7001
dotnet run --project src/ATS.Web      # Web   → https://localhost:7002
```

### Chạy bằng Docker

```bash
docker compose -f docker/docker-compose.yml up --build
```

### Cấu hình khoá AI

**Không bao giờ commit API key.** Dùng user-secrets khi phát triển:

```bash
dotnet user-secrets set "OpenAI:ApiKey" "sk-..." --project src/ATS.Api
```

Khi chạy Docker, truyền qua biến môi trường `OPENAI__APIKEY` trong file `.env` (đã được
`.gitignore` bỏ qua).

### Chạy kiểm thử

```bash
dotnet test
```

---

## 6. Quy trình làm việc

- Nhánh: `main` (ổn định, đã deploy) — `develop` (tích hợp) — `feature/*` (từng việc)
- Mọi thay đổi vào `main`/`develop` phải qua **Pull Request** và có **ít nhất 1 approve** từ
  thành viên khác.
- Backlog và tiến độ quản lý trên **GitHub Projects**: To do → In progress → Review → Done.
- Mỗi việc phải có **issue gán người phụ trách**, gắn nhãn theo tầng và milestone theo tuần.

Chi tiết quy ước: [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 7. Phân công — theo tầng kiến trúc

Nhóm **không** chia mỗi người một module nghiệp vụ. Mỗi thành viên sở hữu trọn **một tầng** và
làm phần tầng đó cho *mọi* chức năng.

| Thành viên | Tầng sở hữu | Project |
|---|---|---|
| TV1 — Hoạt | Nhóm trưởng / Dữ liệu & Hạ tầng | `ATS.Data`, `docker/`, `.github/` |
| TV2 — (điền tên) | Nghiệp vụ & API | `ATS.Business`, `ATS.Api` |
| TV3 — (điền tên) | Giao diện | `ATS.Web` |
| TV4 — (điền tên) | AI & Chất lượng | `ATS.AI`, `ATS.Tests` |

Chi tiết + kế hoạch 10 tuần: [docs/team-assignment.md](docs/team-assignment.md)

---

## 8. Nhật ký công việc

Mỗi thành viên ghi nhật ký hàng tuần vào file riêng trong [docs/worklog/](docs/worklog/).
Đây là minh chứng đóng góp cá nhân khi chấm điểm.

---

## 9. Tài liệu

| Tài liệu | Nội dung |
|---|---|
| [docs/architecture.md](docs/architecture.md) | Kiến trúc nhiều lớp, luồng dữ liệu |
| [docs/database-design.md](docs/database-design.md) | ERD, mô tả bảng |
| [docs/use-cases.md](docs/use-cases.md) | Use case tổng quát |
| [docs/contracts.md](docs/contracts.md) | DTO + interface giữa các tầng |
| [docs/ai-integration.md](docs/ai-integration.md) | Pipeline AI, prompt, chi phí, fallback |
| [docs/team-assignment.md](docs/team-assignment.md) | Phân công theo tầng |
| [docs/weekly-plan.md](docs/weekly-plan.md) | Kế hoạch 10 tuần |
