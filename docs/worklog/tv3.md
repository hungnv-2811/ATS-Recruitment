# Worklog — TV3 — Giao diện

| | |
|---|---|
| **Họ tên** | Hùng |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | Giao diện |
| **Project sở hữu** | `ATS.Web` |

> Mẫu ghi: [_TEMPLATE.md](_TEMPLATE.md). Tuần mới nhất đặt **trên cùng**.

> **Lưu ý khi nộp:** các ô `#—` (Issue/PR) và `~—h` (thời gian) là phần **mỗi người tự điền**
> theo hoạt động thật của mình trên GitHub. Phần "Việc đã làm" ghi đúng trạng thái repo.


---

## Tuần 3–9 (dd/mm – dd/mm) — Giao diện cho cả hai chủ thể

**Chức năng chung:** UI bám theo API ngay trong tuần API xong (contracts.md mục 8 chốt đầu tuần 3).

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ApiClient` + `AuthSession` (JWT trong `ProtectedLocalStorage`, tự đăng xuất khi 401) | #— | #— | Xong |
| UV-1 đăng nhập / đăng ký 2 vai / quên mật khẩu (luôn cùng một câu) / đặt lại mật khẩu | #— | #— | Xong |
| UV-2 quản lý nhiều CV (upload PDF/DOCX ≤ 5 MB, mặc định, xoá, cảnh báo CV không đọc được) | #— | #— | Xong |
| UV-3 danh sách tin (tìm, lọc, phân trang) · UV-4 chi tiết tin + xem độ phù hợp + ứng tuyển | #— | #— | Xong |
| UV-5 lịch sử ứng tuyển với thanh trạng thái | #— | #— | Xong |
| HR-1 quản lý tin (tạo/sửa/đăng/đóng) · HR-2 danh sách xếp hạng + sàng lọc lô có progress | #— | #— | Xong |
| HR-3 chi tiết hồ sơ (CV gốc, điểm AI, chuyển trạng thái) · HR-4 hẹn phỏng vấn + gợi ý câu hỏi · HR-5 đánh giá | #— | #— | Xong |
| HR-6 dashboard · màn quản trị tài khoản · lịch phỏng vấn | #— | #— | Xong |
| `ScoreBadge` dùng chung cho mọi chỗ hiện điểm (nhãn nguồn bắt buộc) · `SecurePage` chặn route theo vai | #— | #— | Xong |

### Vướng mắc

- Máy dev bật Windows Smart App Control nên `dotnet test` trên Windows bị chặn nạp DLL — chạy CI
  local trong WSL Ubuntu thay thế.
- Prerender phải tắt vì phiên đăng nhập nằm trong localStorage.

### Kế hoạch tuần sau

- Chạy thử end-to-end bằng `docker compose` sau khi merge đủ 4 nhánh; chụp màn hình cho PR.

**Thời gian bỏ ra:** ~—h

---

## Tuần 2 (dd/mm – dd/mm) — Dựng khung

**Chức năng chung của tuần:** `docker compose up` chạy được, ArchitectureTests xanh.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ATS.Web` Blazor Server skeleton: `App`, `Routes`, `MainLayout`, `NavMenu`, `Home` | #— | #— | Xong |
| `HttpClient` đọc `ApiBaseUrl` từ config — Web **không** reference project module nào | #— | #— | Xong |
| CSS nền: biến màu + dark mode theo `prefers-color-scheme` | #— | #— | Xong |

### Vướng mắc

- Chưa có API thật để gọi. Từ tuần 3 bám theo API ngay trong tuần API xong, không dồn cuối kỳ.

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
| Wireframe các màn hình, chọn thư viện UI | #— | #— | Chưa bắt đầu |

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
