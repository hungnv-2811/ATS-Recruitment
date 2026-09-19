# ATS.Web

Blazor Server. Giao diện cho **cả Ứng viên và HR**.

## Cấu trúc trang

- `Pages/Candidate/` — dành cho ứng viên (upload CV, xem tin, apply, xem trạng thái)
- `Pages/Hr/` — dành cho HR (đăng tin, xem ứng viên, hẹn phỏng vấn)
- `Shared/` — component tái sử dụng (upload dropzone, progress bar sàng lọc, ...)

Phân quyền UI theo `role` trong JWT.

Fallback: nếu Blazor gặp vấn đề performance, chuyển sang trang Swagger UI cho bảo vệ.
