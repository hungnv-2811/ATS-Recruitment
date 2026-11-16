# Báo cáo công việc hàng tuần

Mỗi thành viên có **một file**, mỗi tuần **một mục** (tuần mới nhất ở trên), theo mẫu
[`_TEMPLATE.md`](_TEMPLATE.md):

| Thành viên | Vai trò | Báo cáo tuần |
|---|---|---|
| **TV1 — Hoạt** (nhóm trưởng) | Kiến trúc & Dữ liệu | [`tv1.md`](tv1.md) |
| **TV2 — Tuấn Anh** | Nghiệp vụ & API (Identity, Recruitment) | [`tv2.md`](tv2.md) |
| **TV3 — Hùng** | Giao diện (Blazor) | [`tv3.md`](tv3.md) |
| **TV4 — Tiền** | AI & Kiểm thử | [`tv4.md`](tv4.md) |

Mỗi mục tuần gồm: việc đã làm (kèm Issue/PR), PR đã review của người khác, vướng mắc, kế
hoạch tuần sau, thời gian bỏ ra. Ngày, link Issue/PR và số giờ do **chính thành viên đó điền**
theo hoạt động thật trên GitHub.

## Tiến độ chung theo tuần

Mốc của từng tuần lấy từ [`weekly-plan.md`](../weekly-plan.md). ✅ = xong.

| Tuần | Mốc | TV1 — Hoạt | TV2 — Tuấn Anh | TV3 — Hùng | TV4 — Tiền | |
|---|---|---|---|---|---|---|
| 1 | Chốt phạm vi & thiết kế | `architecture.md`, `database-design.md` | `use-cases.md`, `contracts.md` | Wireframe (`wireframes.md`) | `ai-integration.md`, prompt v1 | ✅ |
| 2 | Dựng khung | `.sln` 18 project, Persistence, compose, CI | API skeleton, Swagger, JWT | Blazor skeleton | ArchitectureTests (5 quy tắc) | ✅ |
| 3 | Identity | Gửi email SMTP / MailHog | Đăng ký, đăng nhập, quên mật khẩu, JWT | Chốt contract tuần 3; UI đăng nhập/đăng ký/quên mật khẩu | Review PR | ✅ |
| 4 | Tin + nhiều CV | Lưu file CV, trích text PDF/DOCX | Job, CV, API | UI quản lý CV, danh sách tin, quản lý tin của HR | Review PR | ✅ |
| 5 | Ứng tuyển + ẩn danh | Review EF config | Nộp đơn (state machine), `IRecruitmentQueries` | UI chi tiết tin + ứng tuyển, lịch sử | Mở rộng ẩn danh PII + test | ✅ |
| 6 | AI với Fake | Worker host | Danh sách xếp hạng cho HR | UI xếp hạng + tiến độ sàng lọc, chi tiết hồ sơ | Entity AI, Fake adapter, hàng đợi Redis, xử lý hàng đợi | ✅ |
| 7 | Adapter AI thật | Cấu hình AI chung api/worker | Hỗ trợ tích hợp AI | Nhãn nguồn điểm, UI xem độ phù hợp | OpenAI + Keyword, pipeline, cache, hạn mức | ✅ |
| 8 | Phỏng vấn + email | Biến môi trường, CI `dotnet format` | Phỏng vấn, đánh giá, email ngoài transaction | UI hẹn phỏng vấn + câu hỏi, đánh giá | Sinh câu hỏi (OpenAI + Template) | ✅ |
| 9 | Tuần đệm | Dashboard (SQL) | Quản trị tài khoản | Chặn route theo vai, dashboard, quản trị | Embedding, +5 quy tắc kiến trúc, test API AI | ✅ |
| 10 | Tích hợp & hoàn thiện | Migration, seed demo, kiểm Postgres thật | Test DDL, cập nhật contract | Merge, chạy end-to-end, sửa lỗi, thu hồi JWT, test Postgres, khảo sát | 9 test end-to-end | ✅ |

## Kết quả kiểm thử cuối kỳ

| Bộ test | Số test | Nội dung |
|---|---|---|
| `ATS.ArchitectureTests` | 14 | Ranh giới module, port AI chỉ nhận `AnonymizedCv`, API không chạm `DbContext`… |
| `ATS.AiScreening.Tests` | 123 | Pipeline fallback, cache/hạn mức, ẩn danh PII, snapshot chấm điểm |
| `ATS.Recruitment.Tests` | 103 | Nghiệp vụ Identity + Recruitment, thu hồi phiên đăng nhập |
| `ATS.IntegrationTests` | 77 | API qua HTTP, end-to-end, phân quyền chéo, 6 test trên PostgreSQL thật |
| **Tổng** | **317** | CI chạy cả bộ PostgreSQL bằng service `postgres:16` |
