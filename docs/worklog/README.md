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

Mốc của từng tuần lấy từ [`weekly-plan.md`](../weekly-plan.md).

| Tuần | Ngày | Mốc | TV1 — Hoạt | TV2 — Tuấn Anh | TV3 — Hùng | TV4 — Tiền |
|---|---|---|---|---|---|---|
| 1 | 07/09 – 13/09 | Khảo sát & thiết kế | Khảo sát hệ thống tương tự + công nghệ; `architecture.md`, `database-design.md` | Khảo sát hệ thống tương tự, phiếu HR; `use-cases.md`, `contracts.md` | Phiếu ứng viên; `wireframes.md` | Khảo sát công nghệ AI; `ai-integration.md` |
| 2 | 14/09 – 20/09 | Dựng khung | `.sln` 18 project, Persistence, compose, CI | API skeleton, Swagger, JWT | Blazor skeleton | ArchitectureTests (5 quy tắc), `AnonymizedCv` |
| 3 | 21/09 – 27/09 | Identity | Gửi email SMTP / MailHog | Đăng ký, đăng nhập, quên mật khẩu, JWT | Chốt contract; UI đăng nhập/đăng ký | Tách kỹ năng từ JD; review PR |
| 4 | 28/09 – 04/10 | Tin + nhiều CV | Lưu file CV, trích text PDF/DOCX | Job, CV, tạo hồ sơ khi đăng ký | UI CV, danh sách tin, quản lý tin | Mở rộng ẩn danh PII + test |
| 5 | 05/10 – 11/10 | Ứng tuyển + ẩn danh | Khung Worker; review EF | Nộp đơn (state machine), `IRecruitmentQueries` | UI ứng tuyển, lịch sử | Entity AI, EF config, quy tắc kiến trúc |
| 6 | 12/10 – 18/10 | AI với Fake | Vòng lặp Worker đọc hàng đợi | Danh sách xếp hạng, chi tiết hồ sơ | UI xếp hạng + tiến độ, chi tiết hồ sơ | Fake adapter, hàng đợi Redis, xử lý hàng đợi |
| 7 | 19/10 – 25/10 | Adapter AI thật | Cấu hình AI chung api/worker | Test nộp đơn + hỗ trợ tích hợp AI | Nhãn nguồn điểm, UI xem độ phù hợp | OpenAI + Keyword, pipeline, cache, hạn mức |
| 8 | 26/10 – 01/11 | Phỏng vấn + email | Biến môi trường, CI `dotnet format` | Phỏng vấn, đánh giá, email ngoài transaction | UI hẹn phỏng vấn + câu hỏi, đánh giá | Sinh câu hỏi (OpenAI + Template) |
| 9 | 02/11 – 08/11 | Tuần đệm | Dashboard (SQL) | Quản trị tài khoản | Chặn route theo vai, dashboard, làm lại giao diện | Embedding, +5 quy tắc kiến trúc, test API AI |
| 10 | 09/11 – 15/11 | Tích hợp & hoàn thiện | Migration, seed demo, kiểm Postgres thật | Test DDL, cập nhật contract, review | Merge, end-to-end, sửa lỗi, thu hồi JWT, test Postgres | 9 test end-to-end, review |

Trạng thái: tất cả xong, trừ **khảo sát người dùng** (tuần 1) — phiếu đã soạn, kết quả chưa thu.

## Kết quả kiểm thử cuối kỳ

| Bộ test | Số test | Nội dung |
|---|---|---|
| `ATS.ArchitectureTests` | 14 | Ranh giới module, port AI chỉ nhận `AnonymizedCv`, API không chạm `DbContext`… |
| `ATS.AiScreening.Tests` | 123 | Pipeline fallback, cache/hạn mức, ẩn danh PII, snapshot chấm điểm |
| `ATS.Recruitment.Tests` | 103 | Nghiệp vụ Identity + Recruitment, thu hồi phiên đăng nhập |
| `ATS.IntegrationTests` | 77 | API qua HTTP, end-to-end, phân quyền chéo, 6 test trên PostgreSQL thật |
| **Tổng** | **317** | CI chạy cả bộ PostgreSQL bằng service `postgres:16` |
