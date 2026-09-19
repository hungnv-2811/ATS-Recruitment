# Worklog — TV1 — Dữ liệu & Hạ tầng

| | |
|---|---|
| **Họ tên** | Hoạt (nhóm trưởng) |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | Dữ liệu & Hạ tầng |
| **Project sở hữu** | `ATS.SharedKernel`, `ATS.Persistence`, `docker/`, `.github/` |

> Mẫu ghi: [_TEMPLATE.md](_TEMPLATE.md). Tuần mới nhất đặt **trên cùng**.

> **Lưu ý khi nộp:** các ô `#—` (Issue/PR) và `~—h` (thời gian) là phần **mỗi người tự điền**
> theo hoạt động thật của mình trên GitHub. Phần "Việc đã làm" ghi đúng trạng thái repo.


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
