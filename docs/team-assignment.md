# Phân công nhóm — theo tầng kiến trúc

## Nguyên tắc

Nhóm **không** chia mỗi người một module nghiệp vụ. Mỗi thành viên sở hữu trọn **một tầng**
trong kiến trúc nhiều lớp và làm phần tầng đó cho *mọi* chức năng.

```
Chức năng "Ứng tuyển"   ──┐
Chức năng "Quản lý CV"  ──┼── mỗi chức năng đi xuyên qua cả 4 tầng
Chức năng "Sàng lọc AI" ──┘
```

**Lý do:**

1. Ranh giới sở hữu trùng ranh giới kiến trúc → việc tách lớp được thực thi bằng phân công.
2. Mỗi người làm trong một thư mục project riêng → hạn chế xung đột mã nguồn.
3. Mỗi người có một chuyên môn sâu để trả lời khi phản biện.
4. Không nhóm chức năng nào bị bỏ quên, kể cả *Báo cáo & thống kê*.

**Cái giá phải trả:** mỗi chức năng đi qua tay 3 người → nguy cơ tắc dây chuyền.
Chặn bằng **contract-first** (xem [contracts.md](contracts.md)).

## Bảng phân công

| Thành viên | Vai trò | Project sở hữu | Chịu trách nhiệm cuối cùng về |
|---|---|---|---|
| **TV1** — Hoạt | Nhóm trưởng / Kiến trúc & Hạ tầng dữ liệu | `ATS.Data`, `docker/`, `.github/` | Mô hình CSDL, entity, EF Core, migration, repository, tối ưu truy vấn; quản lý repository, branch protection, CI/CD, Docker, triển khai demo |
| **TV2** — (điền tên) | Nghiệp vụ & API | `ATS.Business`, `ATS.Api` | Quy tắc nghiệp vụ, service, validation, state machine tuyển dụng, phân quyền nghiệp vụ; controller, DTO mapping, xử lý lỗi, Swagger |
| **TV3** — (điền tên) | Giao diện & Trải nghiệm | `ATS.Web` | Toàn bộ Blazor: layout, routing, component dùng chung, form, bảng dữ liệu, gọi API, xử lý lỗi phía client, dashboard |
| **TV4** — (điền tên) | AI & Chất lượng | `ATS.AI`, `ATS.Tests` | Pipeline AI 7 bước, prompt, fallback, ước tính chi phí; chiến lược kiểm thử, test tích hợp & E2E |

> Điền đủ **họ tên + MSSV + tài khoản GitHub** để đối chiếu lịch sử commit/PR khi chấm.

## Người dự phòng

| Mảng | Người chính | Người dự phòng |
|---|---|---|
| Dữ liệu & hạ tầng | TV1 | TV2 |
| Nghiệp vụ & API | TV2 | TV1 |
| Giao diện | TV3 | TV2 |
| AI | TV4 | TV1 |

Người dự phòng đọc hiểu mã nguồn của mảng đó và theo dõi các PR liên quan, không cần viết code
khi mọi việc bình thường.

## Đo đóng góp cá nhân

- Mọi việc phải là **issue có assignee**, gắn nhãn `layer:data` / `layer:api` / `layer:web` /
  `layer:ai` và milestone theo tuần.
- Mỗi tuần mỗi người cập nhật [worklog](worklog/) của mình.
- Buổi demo cuối: chọn **một chức năng xuyên suốt**, mỗi người trình bày phần tầng của mình.
- Cuối mỗi tuần mỗi người **trình bày 5 phút** cho cả nhóm về việc tầng mình.

## Rủi ro riêng của cách chia theo tầng

| Rủi ro | Cách chặn |
|---|---|
| Tắc dây chuyền: tầng dưới chậm thì tầng trên đứng | Contract-first tuần 2 + mock/stub bắt buộc |
| TV3 chưa có API để gọi trong tuần 1–3 | Dựng khung Blazor + component dùng chung trên dữ liệu giả |
| TV2 quá tải (nghiệp vụ của cả 5 module) | Giữ controller mỏng; TV1 hỗ trợ từ tuần 6 |
| Không ai nhìn thấy toàn cảnh | Mỗi tuần một buổi *walkthrough* dọc một chức năng qua cả 4 tầng |
| Thầy hỏi thành viên X về AI mà X không biết | Trình bày chéo 5 phút/tuần |
