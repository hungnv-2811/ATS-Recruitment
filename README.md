# ATS-Recruitment — Hệ thống Quản lý Tuyển dụng & Sàng lọc Ứng viên tích hợp AI

Đồ án môn **Chuyên đề tổng hợp (607708)** — Nhóm 4.

Hệ thống ATS (Applicant Tracking System) giúp bộ phận nhân sự quản lý tin tuyển dụng, hồ sơ
ứng viên và quy trình phỏng vấn; đồng thời tích hợp **trợ lý AI** để **tóm tắt CV** và **chấm
độ phù hợp CV–JD**, hỗ trợ HR sàng lọc hàng trăm hồ sơ nhanh và khách quan hơn.

---

## 1. Chức năng

| #            | Nhóm chức năng                   | Mô tả                                                                                             |
| ------------ | ----------------------------------- | --------------------------------------------------------------------------------------------------- |
| 1            | Tài khoản & phân quyền          | Đăng ký/đăng nhập, phân quyền Quản trị / HR / Nhà tuyển dụng                           |
| 2            | Quản lý tin tuyển dụng (Job/JD) | Tạo, sửa, đóng tin; mô tả công việc và yêu cầu                                           |
| 3            | Quản lý ứng viên & CV           | Hồ sơ ứng viên, tải lên CV, lưu trữ, tìm kiếm                                             |
| 4            | Quy trình tuyển dụng             | Ứng tuyển, vòng phỏng vấn, chuyển trạng thái, lịch hẹn                                    |
| 5            | Báo cáo & thống kê              | Dashboard số liệu tuyển dụng, xuất báo cáo                                                   |
| **AI** | **Trợ lý sàng lọc**       | **Tóm tắt CV + chấm điểm phù hợp CV–JD (0–100) + xếp hạng ứng viên kèm lý do** |

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

**Modular Monolith** — một tiến trình API chứa các module nghiệp vụ có ranh giới rõ, cộng một
**worker chạy nền** cho phần sàng lọc AI:

```
                   ┌──────────────────────────────────────────┐
   Web app HR ────►│  API — modular monolith (ASP.NET Core)   │────► PostgreSQL
   (ATS.Web)       │                                          │
                   │  Tuyển dụng │ Ứng viên │ Phỏng vấn │ AI  │
                   └──────────────────────────────┬───────────┘
                                                  │ đẩy việc
                                                  ▼
                                        ┌──────────────────┐
                                        │  Hàng đợi Redis  │
                                        └────────┬─────────┘
                                                 ▼
                                        ┌──────────────────┐      ┌──────────────┐
                                        │  AI worker       │─────►│ LLM provider │
                                        │  (ATS.AI)        │      │ qua adapter  │
                                        └────────┬─────────┘      └──────────────┘
                                                 └────► PostgreSQL (ghi điểm)

  Chiều ngang (tầng, ai sở hữu):  ATS.Web → ATS.Api → ATS.Business → ATS.Data
  Chiều dọc  (module nghiệp vụ):  Modules/{Recruitment, Candidates, Hiring, Reporting, AiScreening}
  ATS.Contracts — DTO + interface dùng chung   ·   ATS.Tests — xUnit + test ranh giới kiến trúc
```

**Ba quyết định kiến trúc** (đầy đủ kèm phương án đã loại và đánh đổi:
[docs/architecture.md](docs/architecture.md)):

1. **Modular Monolith**, không phải microservices — chi phí vận hành vượt xa nhu cầu ở quy mô
   này (write QPS < 5), trong khi nhóm 4 người/10 tuần không có ai vận hành hệ phân tán.
2. **Sàng lọc AI chạy bất đồng bộ** qua hàng đợi + worker — một lô 300 CV mất 15–40 phút, không
   thể nằm trong một HTTP request. Có cache, retry, và fallback ba cấp khi LLM hỏng.
3. **LLM đứng sau port/adapter** — đổi nhà cung cấp bằng một dòng cấu hình; bước ẩn danh PII được
   trình biên dịch cưỡng chế (port nhận kiểu `AnonymizedCv`, không nhận `string`).

---

## 3. Công nghệ

| Thành phần | Công nghệ                                                  |
| ------------ | ------------------------------------------------------------ |
| Backend      | ASP.NET Core Web API (.NET 8), C#                            |
| CSDL         | Entity Framework Core + **PostgreSQL 16** (Npgsql)           |
| Hàng đợi   | Redis (bền — bật AOF)                                       |
| Giao diện   | Blazor                                                       |
| AI           | OpenAI/Azure OpenAI —`text-embedding-3` + `gpt-4o-mini`, gọi qua port/adapter |
| DevOps       | Docker, docker-compose, GitHub Actions                       |
| Kiểm thử   | xUnit, Moq, NetArchTest (cưỡng chế ranh giới module)      |

> **Vì sao PostgreSQL chứ không phải SQL Server:** ảnh Docker nhẹ hơn ~6 lần và khởi động
> trong ~3 giây thay vì 30–60 giây — nhân với số lần chạy CI và `docker compose up` mỗi ngày
> trong 10 tuần thì chênh lệch này lớn hơn nhiều so với lợi thế quen SSMS. Ngoài ra chạy được
> native trên Mac Apple Silicon, và có `pgvector` nếu về sau muốn lưu embedding trong CSDL.
> Điều kiện để giữ khả năng đổi: **không viết raw SQL trong `ATS.Data`**.

---

## 4. Cấu trúc thư mục

```
ATS-Recruitment/
├── README.md
├── CONTRIBUTING.md              ← quy ước branch / commit / PR bắt buộc
├── .dockerignore                ← chặn bin/obj và bí mật lọt vào image
├── .github/
│   ├── workflows/ci.yml         ← CI: build + test + cảnh báo PR quá lớn
│   ├── CODEOWNERS               ← tự gán reviewer theo tầng
│   ├── pull_request_template.md
│   └── ISSUE_TEMPLATE/task.md
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
├── docs/
│   ├── architecture.md          ← ràng buộc, 3 quyết định kiến trúc, đánh đổi
│   ├── code-review.md           ← tiêu chí review theo từng tầng
│   ├── database-design.md       ← ERD, mô tả bảng
│   ├── use-cases.md             ← use case tổng quát
│   ├── contracts.md             ← DTO + interface giữa các tầng
│   ├── ai-integration.md        ← pipeline AI, prompt, chi phí, fallback
│   ├── team-assignment.md       ← phân công theo tầng
│   ├── weekly-plan.md           ← kế hoạch 10 tuần
│   └── worklog/                 ← nhật ký công việc từng thành viên
└── src/
    ├── ATS.Contracts/           [cả nhóm — báo trước khi sửa]
    ├── ATS.Data/                [TV1]
    ├── ATS.Business/            [TV2]
    ├── ATS.Api/                 [TV2]
    ├── ATS.Web/                 [TV3]
    ├── ATS.AI/                  [TV4] ← thư viện pipeline + host worker chạy nền
    └── ATS.Tests/               [TV4]
```

---

## 5. Cách chạy

### Yêu cầu

- .NET SDK 8.0
- Docker Desktop — dùng cho PostgreSQL và Redis kể cả khi phát triển ở máy

### Chạy khi phát triển

```bash
# 1. Dựng hạ tầng (PostgreSQL + Redis), chưa build ứng dụng
docker compose -f docker/docker-compose.yml up -d db queue

# 2. Áp migration
dotnet restore
dotnet ef database update --project src/ATS.Data --startup-project src/ATS.Api

# 3. Chạy ba tiến trình ở ba terminal
dotnet run --project src/ATS.Api      # API    → https://localhost:7001
dotnet run --project src/ATS.Web      # Web    → https://localhost:7002
dotnet run --project src/ATS.AI       # Worker → không có cổng, đọc hàng đợi
```

Không chạy worker thì hệ thống **vẫn dùng được bình thường**, chỉ là yêu cầu sàng lọc nằm mãi ở
trạng thái *Đang chờ* — đúng theo thiết kế ở ADR-02.

### Chạy bằng Docker

```bash
cp docker/.env.example docker/.env        # rồi điền OPENAI_API_KEY
docker compose -f docker/docker-compose.yml up --build

# Lô CV lớn thì tăng số worker, API giữ nguyên một bản:
docker compose -f docker/docker-compose.yml up --scale worker=3
```

| Container | Cổng | Vai trò |
|---|---|---|
| `web` | 8081 | Giao diện HR |
| `api` | 8080 | Nghiệp vụ + REST API |
| `worker` | — | Chạy pipeline AI, không mở cổng |
| `db` | 5432 | PostgreSQL |
| `queue` | 6379 | Redis |

### Cấu hình khoá AI

**Không bao giờ commit API key.** Khoá chỉ cần cho **worker** — `ATS.Api` không bao giờ gọi
thẳng LLM (xem ADR-02, ADR-03), nên đừng cấu hình khoá ở đó.

```bash
dotnet user-secrets set "OpenAI:ApiKey" "sk-..." --project src/ATS.AI
```

Khi chạy Docker, truyền qua `OPENAI_API_KEY` trong `docker/.env` (đã được `.gitignore` bỏ qua).
Đổi nhà cung cấp bằng biến `AI_PROVIDER` (`OpenAI` | `AzureOpenAI` | `LocalModel` |
`EmbeddingOnly` | `Fake`) — không phải sửa code nghiệp vụ.

### Chạy kiểm thử

```bash
dotnet test
```

---

## 6. Quy trình làm việc

- Nhánh: `main` (ổn định, đã deploy) — `develop` (tích hợp) — `feature/*` (từng việc)
- **Mọi thay đổi vào `develop` và `main` đều phải qua Pull Request và có ít nhất 1 approve.**
  Không commit thẳng, kể cả sửa một dòng. PR chạm `ATS.Contracts` cần 2 approve.
- Reviewer mặc định là **người tiêu thụ đầu ra của tầng bạn** — [.github/CODEOWNERS](.github/CODEOWNERS)
  tự gán. Cam kết phản hồi review trong **24 giờ**.
- Điều kiện merge: CI xanh + đủ approve + có `Closes #N` + PR dưới ~400 dòng.
- Backlog và tiến độ quản lý trên **GitHub Projects**: To do → In progress → Review → Done.
- Mỗi việc phải có **issue gán người phụ trách**, gắn nhãn theo tầng và milestone theo tuần.

Chi tiết quy ước: [CONTRIBUTING.md](CONTRIBUTING.md) · Tiêu chí review từng tầng:
[docs/code-review.md](docs/code-review.md)

---

## 7. Phân công — theo tầng kiến trúc

Nhóm **không** chia mỗi người một module nghiệp vụ. Mỗi thành viên sở hữu trọn **một tầng** và
làm phần tầng đó cho *mọi* chức năng.

| Thành viên     | Tầng sở hữu                         | Project                                 |
| ---------------- | -------------------------------------- | --------------------------------------- |
| TV1 — Hoạt     | Nhóm trưởng / Dữ liệu & Hạ tầng | `ATS.Data`, `docker/`, `.github/` |
| TV2 — Tuấn Anh | Nghiệp vụ & API                      | `ATS.Business`, `ATS.Api`           |
| TV3 — Hùng     | Giao diện                             | `ATS.Web`                             |
| TV4 — Tiền     | AI & Chất lượng                     | `ATS.AI`, `ATS.Tests`               |

Chi tiết + kế hoạch 10 tuần: [docs/team-assignment.md](docs/team-assignment.md)

---

## 8. Nhật ký công việc

Mỗi thành viên ghi nhật ký hàng tuần vào file riêng trong [docs/worklog/](docs/worklog/).
Đây là minh chứng đóng góp cá nhân khi chấm điểm.

---

## 9. Tài liệu

| Tài liệu                                        | Nội dung                                 |
| ------------------------------------------------- | ----------------------------------------- |
| [docs/architecture.md](docs/architecture.md)       | Ràng buộc, 3 quyết định kiến trúc, đánh đổi |
| [docs/code-review.md](docs/code-review.md)         | Tiêu chí code review theo từng tầng     |
| [docs/database-design.md](docs/database-design.md) | ERD, mô tả bảng                        |
| [docs/use-cases.md](docs/use-cases.md)             | Use case tổng quát                      |
| [docs/contracts.md](docs/contracts.md)             | DTO + interface giữa các tầng          |
| [docs/ai-integration.md](docs/ai-integration.md)   | Pipeline AI, prompt, chi phí, fallback   |
| [docs/team-assignment.md](docs/team-assignment.md) | Phân công theo tầng                    |
| [docs/weekly-plan.md](docs/weekly-plan.md)         | Kế hoạch 10 tuần                       |
