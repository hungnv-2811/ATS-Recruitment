# Worklog — TV3 — Giao diện

| | |
|---|---|
| **Họ tên** | Hùng |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | Giao diện |
| **Project sở hữu** | `ATS.Web` |

> **Cách đọc:** mỗi tuần một mục, tuần mới nhất ở trên. Việc được xếp vào tuần theo
> [`weekly-plan.md`](../weekly-plan.md); tuần 1 bắt đầu 07/09/2026. Các ô `#—` (Issue/PR) và
> `~—h` (thời gian) mỗi người **tự điền theo hoạt động thật** của mình trên GitHub.

---

## Tuần 10 (09/11 – 15/11) — Tích hợp, kiểm thử trên hệ thống thật, hoàn thiện

**Chức năng chung của tuần:** Merge đủ các nhánh, migration + seed, chạy `docker compose` end-to-end, sửa lỗi phát hiện.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Merge 4 nhánh, CI trong WSL, `docker compose` end-to-end; sửa trang trắng trong container (`blazor.web.js`) | #— | #— | Xong |
| Nộp đơn trùng đồng thời → 409; thu hồi JWT bằng security stamp; email có dấu + giờ VN (chờ TV2 review) | #— | #— | Xong |
| Bộ test trên Postgres thật + service Postgres trong CI (chờ TV1/TV4 review); README kịch bản demo | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- Windows Smart App Control chặn `dotnet test` → chạy test trong WSL Ubuntu.

### Kế hoạch tuần sau

- Bảo vệ đồ án.

**Thời gian bỏ ra:** ~—h

---

## Tuần 9 (02/11 – 08/11) — Tuần đệm — Embedding, báo cáo, phân quyền UI

**Chức năng chung của tuần:** Trả nợ tuần 7–8, thêm Embedding + dashboard, chặn route theo vai, hoàn thiện giao diện.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `SecurePage`: chặn route theo vai (sai vai → 403); HR-6 dashboard; quản trị tài khoản | #— | #— | Xong |
| Làm lại giao diện (topbar, trang chủ, thẻ, bảng, form), co giãn màn hình hẹp; nhãn “điểm cũ, đang chấm lại” | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Merge 4 nhánh, CI trong WSL, `docker compose` end-to-end; sửa trang trắng trong container (`blazor.web.js`)

**Thời gian bỏ ra:** ~—h

---

## Tuần 8 (26/10 – 01/11) — Phỏng vấn + gợi ý câu hỏi + email

**Chức năng chung của tuần:** HR hẹn phỏng vấn, AI gợi ý câu hỏi, MailHog nhận thư; SMTP lỗi vẫn lưu buổi phỏng vấn.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| HR-4 hẹn phỏng vấn + gợi ý câu hỏi (sửa/xoá/thêm tay) + gửi lại lời mời | #— | #— | Xong |
| HR-5 nhập đánh giá; lịch phỏng vấn của HR | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- `SecurePage`: chặn route theo vai (sai vai → 403); HR-6 dashboard; quản trị tài khoản

**Thời gian bỏ ra:** ~—h

---

## Tuần 7 (19/10 – 25/10) — Adapter AI thật — nhiều tầng, cache, hạn mức

**Chức năng chung của tuần:** Pipeline thật có fallback; xem độ phù hợp lần 2 trúng cache, không trừ lượt.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ScoreBadge` dùng chung: điểm luôn kèm nhãn nguồn; cảnh báo bảng trộn nguồn | #— | #— | Xong |
| UV-4 xem độ phù hợp: lượt còn lại, huỷ được, hết lượt vẫn ứng tuyển, “đã xem trước đó” | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- HR-4 hẹn phỏng vấn + gợi ý câu hỏi (sửa/xoá/thêm tay) + gửi lại lời mời

**Thời gian bỏ ra:** ~—h

---

## Tuần 6 (12/10 – 18/10) — AiScreening với FakeAdapter (end-to-end)

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

- `ScoreBadge` dùng chung: điểm luôn kèm nhãn nguồn; cảnh báo bảng trộn nguồn

**Thời gian bỏ ra:** ~—h

---

## Tuần 5 (05/10 – 11/10) — Ứng tuyển + ẩn danh CV

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

## Tuần 4 (28/09 – 04/10) — Recruitment — tin tuyển dụng, nhiều CV

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

## Tuần 3 (21/09 – 27/09) — Identity — đăng ký/đăng nhập 2 vai, quên mật khẩu

**Chức năng chung của tuần:** Đăng ký ứng viên → đăng nhập → nhận JWT → gọi `/api/me`; quên mật khẩu qua MailHog.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Chốt contract chi tiết tuần 3 (`contracts.md` mục 8), nối sẵn các module vào `Program.cs` | #— | #— | Xong |
| `ApiClient` (gắn JWT, đổi lỗi API thành câu tiếng Việt) + `AuthSession` | #— | #— | Xong |
| UV-1: đăng nhập, đăng ký 2 vai, quên mật khẩu, đặt lại mật khẩu | #— | #— | Xong |

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

## Tuần 2 (14/09 – 20/09) — Dựng khung

**Chức năng chung của tuần:** Clone repo, `docker compose up` thấy API xanh, ArchitectureTests pass.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ATS.Web` Blazor Server skeleton: layout, routing, `HttpClient` đọc `ApiBaseUrl` | #— | #— | Xong |
| CSS nền: biến màu + dark mode | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- —

### Kế hoạch tuần sau

- Chốt contract chi tiết tuần 3 (`contracts.md` mục 8), nối sẵn các module vào `Program.cs`

**Thời gian bỏ ra:** ~—h

---

## Tuần 1 (07/09 – 13/09) — Khảo sát, chốt phạm vi & thiết kế

**Chức năng chung của tuần:** Khảo sát hệ thống tương tự, người dùng, công nghệ; tài liệu thiết kế đóng, cả nhóm thống nhất ranh giới module và API.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Phiếu khảo sát ứng viên (U1–U10) — `khao-sat.md` mục 2.2 | #— | #— | Đang làm |
| Wireframe ứng viên & HR, bốn quy tắc chung cho mọi màn hình — `wireframes.md` | #— | #— | Xong |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

- Phiếu ứng viên mới soạn, chưa đủ 30 phiếu trả lời.

### Kế hoạch tuần sau

- `ATS.Web` Blazor Server skeleton: layout, routing, `HttpClient` đọc `ApiBaseUrl`

**Thời gian bỏ ra:** ~—h

---
