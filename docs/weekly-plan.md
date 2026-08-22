# Kế hoạch 10 tuần

Mỗi tuần cả nhóm cùng làm **một nhóm chức năng**, mỗi người làm phần tầng của mình.

| Tuần | Chức năng chung | TV1 — Data & Hạ tầng | TV2 — Nghiệp vụ & API | TV3 — Giao diện | TV4 — AI & Test |
|---|---|---|---|---|---|
| 1 | Khởi động | Tạo repo, cấu trúc solution, branch protection, board | Phân tích yêu cầu, use case tổng quát | Wireframe màn hình, chọn thư viện UI | Nghiên cứu pipeline AI, đăng ký API key |
| 2 | **Nền tảng + Contract** | ERD, entity, `DbContext`, migration đầu, CI cơ bản | **Chủ trì chốt `ATS.Contracts`** | Khung Blazor, layout, mock service | **Demo AI cốt lõi** (console) |
| 3 | Tài khoản & phân quyền | Bảng User/Role, seed | AuthService, JWT, policy phân quyền | Trang đăng nhập/đăng ký, lưu token | Unit test auth; trích xuất text PDF |
| 4 | Tin tuyển dụng (Job/JD) | Entity Job, migration, index tìm kiếm | JobService + JobController | Trang danh sách / chi tiết / form Job | Test Job; bước ẩn danh dữ liệu |
| 5 | Ứng viên & CV | Entity Candidate/CV, lưu file, metadata | CvService (upload, trích text), controller | Trang upload CV, danh sách ứng viên | Test upload; embedding + cosine |
| 6 | Quy trình tuyển dụng | Entity Application, Interview | State machine + quy tắc chuyển trạng thái | Màn hình luồng ứng tuyển | Test state machine; prompt tóm tắt + lý do |
| 7 | **Tích hợp AI** | Bảng `AI_Scores`, cache, log | Gọi `IAiScoringService`, lưu kết quả, xếp hạng | Màn hình xếp hạng + nút phản hồi HR | **Bàn giao `ATS.AI` + fallback** |
| 8 | Docker & triển khai | **Dockerfile, compose, deploy demo** | Hoàn thiện API, chuẩn hoá xử lý lỗi | Hoàn thiện UI, responsive | Test tích hợp trên bản deploy |
| 9 | Báo cáo & ổn định | Truy vấn thống kê, tối ưu index | API báo cáo & thống kê | Dashboard biểu đồ, xuất báo cáo | **Test E2E, hồi quy, sửa lỗi** |
| 10 | Hoàn thiện | Tài liệu kiến trúc + CI/CD | Tài liệu API | Slide + kịch bản demo | `ai-integration.md` + báo cáo kiểm thử |

## Mốc bàn giao

| Mốc | Sản phẩm |
|---|---|
| Cuối tuần 1 | Báo cáo tổng quan + repo đã khởi tạo + board backlog |
| **Cuối tuần 2** | Sơ đồ CSDL + **`ATS.Contracts` đã chốt** + demo AI cốt lõi |
| Cuối tuần 7 | Dịch vụ AI hoàn chỉnh có fallback |
| Cuối tuần 8 | Bản deploy chạy được |
| Cuối tuần 10 | Báo cáo cuối + slide + demo |
