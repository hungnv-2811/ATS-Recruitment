# Worklog — TV3 — Giao diện

| | |
|---|---|
| **Họ tên** | Hùng |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | Giao diện |
| **Project sở hữu** | `ATS.Web` |

> **Cách đọc:** mỗi tuần một mục, tuần mới nhất ở trên. Việc được xếp vào tuần theo
> [`weekly-plan.md`](../weekly-plan.md). Các ô `dd/mm`, `#—` (Issue/PR) và `~—h` (thời gian)
> mỗi người **tự điền theo hoạt động thật** của mình trên GitHub.


---

## Tuần 10 (dd/mm – dd/mm) — Tích hợp, kiểm thử trên hệ thống thật, hoàn thiện

**Chức năng chung của tuần:** Merge đủ các nhánh, migration + seed, chạy `docker compose` end-to-end, sửa lỗi phát hiện.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Merge 4 nhánh vào `nvhung`, chạy CI trong WSL, chạy `docker compose` end-to-end theo kịch bản demo | #— | #— | Xong |
| Sửa lỗi trang trắng trong container (`blazor.web.js` thiếu khi publish) | #— | #— | Xong |
| Sửa nộp đơn trùng đồng thời → 409 thay vì 500 (file của TV2, cần TV2 review) | #— | #— | Xong |
| Thu hồi JWT khi khoá tài khoản/đổi vai/đổi mật khẩu bằng security stamp (file của TV2, cần TV2 review) | #— | #— | Xong |
| Email có dấu + giờ Việt Nam (file của TV2); nhãn “điểm cũ, đang chấm lại” | #— | #— | Xong |
| Bộ test trên Postgres thật + service Postgres trong CI (file của TV1/TV4, cần review) | #— | #— | Xong |
| Làm lại giao diện (topbar, trang chủ, thẻ, bảng, form); tài liệu khảo sát `docs/khao-sat.md` | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- Máy Windows bật Smart App Control chặn `dotnet test` → chạy test trong WSL Ubuntu.

### Kế hoạch tuần sau

- Bảo vệ đồ án.

**Thời gian bỏ ra:** ~—h

---

## Tuần 9 (dd/mm – dd/mm) — Tuần đệm — Embedding, báo cáo, phân quyền UI

**Chức năng chung của tuần:** Trả nợ tuần 7–8, thêm Embedding + dashboard, chặn route theo vai.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `SecurePage`: chặn route theo vai (sai vai → 403, chưa đăng nhập → /login) | #— | #— | Xong |
| HR-6 dashboard, màn quản trị tài khoản, giao diện co giãn màn hình hẹp | #— | #— | Xong |
| `wireframes.md` mục 5 (route thật), README | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Merge 4 nhánh vào `nvhung`, chạy CI trong WSL, chạy `docker compose` end-to-end theo kịch bản demo

**Thời gian bỏ ra:** ~—h

---

## Tuần 8 (dd/mm – dd/mm) — Phỏng vấn + gợi ý câu hỏi + email

**Chức năng chung của tuần:** HR hẹn phỏng vấn, AI gợi ý câu hỏi, MailHog nhận thư; SMTP lỗi vẫn lưu buổi phỏng vấn.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| HR-4 hẹn phỏng vấn + gợi ý câu hỏi (sửa/xoá/thêm tay) + nút gửi lại lời mời | #— | #— | Xong |
| HR-5 nhập đánh giá; lịch phỏng vấn của HR | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `SecurePage`: chặn route theo vai (sai vai → 403, chưa đăng nhập → /login)

**Thời gian bỏ ra:** ~—h

---

## Tuần 7 (dd/mm – dd/mm) — Adapter AI thật — chấm điểm nhiều tầng, cache, hạn mức

**Chức năng chung của tuần:** Pipeline thật có fallback; preview lần 2 trúng cache, không trừ lượt.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ScoreBadge` dùng chung: điểm luôn kèm nhãn nguồn (AI / Ngữ nghĩa / Từ khoá); cảnh báo bảng trộn nguồn | #— | #— | Xong |
| UV-4 xem độ phù hợp: số lượt còn lại, huỷ được, hết lượt vẫn ứng tuyển được, “đã xem trước đó” | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- HR-4 hẹn phỏng vấn + gợi ý câu hỏi (sửa/xoá/thêm tay) + nút gửi lại lời mời

**Thời gian bỏ ra:** ~—h

---

## Tuần 6 (dd/mm – dd/mm) — AiScreening với FakeAdapter (end-to-end)

**Chức năng chung của tuần:** Nộp đơn → hàng đợi → Worker → điểm lưu DB, dùng Fake để không tốn tiền.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| HR-2 danh sách xếp hạng + `ScreeningProgress` (polling 2s, quay lại trang không mất tiến độ) | #— | #— | Xong |
| HR-3 chi tiết hồ sơ: xem/tải CV gốc, điểm AI, chuyển trạng thái | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `ScoreBadge` dùng chung: điểm luôn kèm nhãn nguồn (AI / Ngữ nghĩa / Từ khoá); cảnh báo bảng trộn nguồn

**Thời gian bỏ ra:** ~—h

---

## Tuần 5 (dd/mm – dd/mm) — Ứng tuyển + ẩn danh CV

**Chức năng chung của tuần:** Ứng viên nộp CV vào tin và thấy trong lịch sử; CV được ẩn danh trước khi tới AI.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| UV-4 chi tiết tin + ứng tuyển; UV-5 lịch sử ứng tuyển với thanh trạng thái | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- HR-2 danh sách xếp hạng + `ScreeningProgress` (polling 2s, quay lại trang không mất tiến độ)

**Thời gian bỏ ra:** ~—h

---

## Tuần 4 (dd/mm – dd/mm) — Recruitment — tin tuyển dụng, nhiều CV

**Chức năng chung của tuần:** Ứng viên upload nhiều CV, HR đăng tin, cả hai xem được.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| UV-2 quản lý nhiều CV (upload, mặc định, xoá, cảnh báo CV không đọc được) | #— | #— | Xong |
| UV-3 danh sách tin (tìm, lọc, phân trang); HR-1 quản lý tin (tạo/sửa/đăng/đóng) | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- UV-4 chi tiết tin + ứng tuyển; UV-5 lịch sử ứng tuyển với thanh trạng thái

**Thời gian bỏ ra:** ~—h

---

## Tuần 3 (dd/mm – dd/mm) — Identity — đăng ký/đăng nhập 2 vai, quên mật khẩu

**Chức năng chung của tuần:** Đăng ký ứng viên → đăng nhập → nhận JWT → gọi `/api/me`; quên mật khẩu qua MailHog.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Chốt contract chi tiết tuần 3 (`contracts.md` mục 8) và nối sẵn các module vào `Program.cs` để 4 người làm song song | #— | #— | Xong |
| `ApiClient` (gắn JWT, đổi lỗi API thành câu tiếng Việt) + `AuthSession` (JWT trong `ProtectedLocalStorage`) | #— | #— | Xong |
| UV-1: đăng nhập, đăng ký 2 vai, quên mật khẩu (luôn cùng một câu), đặt lại mật khẩu | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- Phiên đăng nhập nằm trong localStorage → phải tắt prerender.

### Kế hoạch tuần sau

- UV-2 quản lý nhiều CV (upload, mặc định, xoá, cảnh báo CV không đọc được)

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
