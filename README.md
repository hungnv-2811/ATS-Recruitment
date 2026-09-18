# ATS-Recruitment — Nền tảng tuyển dụng 2 phía với AI trợ lý

Đồ án môn **Chuyên đề tổng hợp (607708)** — Nhóm 3.

Hệ thống hỗ trợ **HR** và **Ứng viên** cùng lúc: ứng viên quản lý nhiều CV và tự ứng tuyển; HR
đăng tin và sàng lọc hồ sơ. Ở giữa là **AI trợ lý hai chiều**: chấm điểm phù hợp CV ↔ JD cho cả
hai phía nhìn thấy, và sinh bộ câu hỏi phỏng vấn từ hồ sơ của ứng viên khi HR lên lịch phỏng vấn.

Điểm khác biệt so với các cổng tuyển dụng phổ biến (TopCV, ITviec, VietnamWorks) không nằm ở
"chợ việc" — mà ở lớp AI phục vụ song song HR và ứng viên, trên nền **bảo vệ dữ liệu cá nhân
được ép ở mức compiler** (kiểu `AnonymizedCv` — không có PII thì mới gửi được lên LLM).

---

## 1. Chủ thể và luồng chính

Hệ thống có **2 chủ thể nghiệp vụ** (`HR`, `Ứng viên`) và **1 vai kỹ thuật** (`Quản trị viên` —
chỉ quản lý tài khoản, không tham gia nghiệp vụ tuyển dụng).

| Chủ thể     | Làm gì                                                                                      |
|-------------|---------------------------------------------------------------------------------------------|
| Ứng viên    | Đăng ký, quản lý **nhiều CV**, xem tin, nộp CV vào tin (biết % phù hợp trước khi nộp), xem trạng thái |
| HR          | Đăng/sửa/đóng tin, xem danh sách ứng viên đã được AI xếp hạng, chuyển trạng thái, hẹn phỏng vấn (có AI gợi ý câu hỏi), nhập đánh giá |
| Quản trị viên | Quản lý tài khoản, phân quyền                                                             |

Luồng cốt lõi (một chu trình):

```
Ứng viên nộp CV vào tin
  → AI ẩn danh CV, chấm % phù hợp (cả 2 phía cùng thấy)
  → HR xem danh sách đã xếp hạng
  → HR hẹn phỏng vấn
  → AI sinh bộ câu hỏi theo JD + CV của người đó
  → HR nhập đánh giá
  → Chuyển trạng thái ứng tuyển
```

## 2. Chức năng

| Nhóm | Chức năng |
|---|---|
| Tài khoản | Đăng ký/đăng nhập (Ứng viên & HR), phân quyền, quên mật khẩu |
| CV của Ứng viên | Upload nhiều CV (PDF/DOCX), đặt tên, chọn CV mặc định, xóa |
| Tin tuyển dụng | HR tạo/sửa/đóng tin; ứng viên xem, tìm kiếm, lọc |
| Ứng tuyển | Ứng viên chọn 1 trong các CV → nộp vào tin; xem lịch sử |
| **AI — chấm phù hợp** | Ẩn danh CV → LLM chấm điểm 0–100 + lý do; hiển thị cho cả HR và ứng viên |
| Sàng lọc hàng loạt | HR chạy sàng lọc AI cho toàn bộ hồ sơ của một tin (nền, theo lô) |
| Phỏng vấn | HR xếp lịch; **AI sinh câu hỏi phỏng vấn** từ JD + CV; nhập đánh giá |
| Trạng thái ứng tuyển | Đã nộp → Sàng lọc → Phỏng vấn → Nhận / Từ chối |
| Báo cáo | Dashboard số hồ sơ, tỉ lệ chuyển đổi, tuyển dụng theo tin |

## 3. Kiến trúc — một dòng

> **Modular Monolith theo Clean Architecture, áp dụng Ports & Adapters cho hai năng lực AI
> (chấm phù hợp và sinh câu hỏi phỏng vấn)** — tách domain khỏi hạ tầng, thay thế nhà cung cấp
> AI được, kiểm thử không phụ thuộc dịch vụ ngoài.

3 module nghiệp vụ: `Recruitment` (Jobs + Candidates + CVs + Applications + Interviews),
`AiScreening` (2 port AI + adapter thật + fallback), `Identity` (User cho HR và Ứng viên, JWT).

Chi tiết thiết kế và lý do trong [`docs/architecture.md`](docs/architecture.md).

## 4. Công nghệ

- **Backend**: ASP.NET Core 8, EF Core, PostgreSQL 16
- **Queue**: Redis 7 (sàng lọc AI bất đồng bộ)
- **Frontend**: Blazor Server (fallback: Swagger UI cho demo)
- **AI**: OpenAI SDK (chính) + Embedding + Keyword matching (fallback 3 tầng)
- **Deploy**: Docker Compose (db, queue, api, worker, web)

## 5. Cấu trúc thư mục

```
ATS-Recruitment/
├── src/
│   ├── Shared/ATS.SharedKernel/          # Entity, ValueObject, Result<T>
│   ├── Modules/
│   │   ├── Recruitment/                  # 3 project: Domain, Application, Infrastructure
│   │   ├── AiScreening/                  # 3 project: 2 port AI + pipeline fallback
│   │   └── Identity/                     # 3 project: User, JWT
│   ├── Hosts/
│   │   ├── ATS.Api/                      # Web API + Composition Root
│   │   ├── ATS.Worker/                   # Consumer đọc Redis queue
│   │   └── ATS.Web/                      # Blazor
│   └── Tests/
│       ├── ATS.AiScreening.Tests/
│       ├── ATS.ArchitectureTests/        # NetArchTest ép ranh giới
│       └── ATS.IntegrationTests/
├── docker/                               # Dockerfile + docker-compose.yml
├── docs/                                 # Tài liệu thiết kế
├── .github/                              # CI, CODEOWNERS, templates
└── ATS.sln
```

## 6. Tài liệu

| File | Nội dung |
|---|---|
| [`docs/architecture.md`](docs/architecture.md) | Kiến trúc: ràng buộc, 3 quyết định lớn, đánh đổi |
| [`docs/use-cases.md`](docs/use-cases.md) | Tác nhân, use case, sơ đồ, trạng thái |
| [`docs/database-design.md`](docs/database-design.md) | ERD, bảng, quan hệ, khóa ngoại |
| [`docs/contracts.md`](docs/contracts.md) | Port/Interface & DTO (contract-first) |
| [`docs/ai-integration.md`](docs/ai-integration.md) | Prompt, pipeline, fallback, chi phí |
| [`docs/weekly-plan.md`](docs/weekly-plan.md) | Kế hoạch 10 tuần theo milestone |
| [`docs/team-assignment.md`](docs/team-assignment.md) | Phân công 4 thành viên theo module |
| [`docs/code-review.md`](docs/code-review.md) | Checklist review |
| [`CONTRIBUTING.md`](CONTRIBUTING.md) | Nhánh, commit, PR |

## 7. Chạy thử (khi có code)

```bash
docker compose -f docker/docker-compose.yml up -d
# API:    http://localhost:5000
# Web:    http://localhost:5001
# Swagger: http://localhost:5000/swagger
```

## 8. Nhóm

Nhóm 3 — 4 thành viên, xem [`docs/team-assignment.md`](docs/team-assignment.md).
