# Use case tổng quát

> TV2 phụ trách tài liệu này. Cập nhật trong tuần 1.

## 1. Tác nhân

| Tác nhân | Mô tả |
|---|---|
| Quản trị viên | Quản lý tài khoản, phân quyền |
| HR | Đăng tin, quản lý ứng viên, sàng lọc, xếp lịch phỏng vấn |
| Nhà tuyển dụng | Xem ứng viên của tin mình phụ trách, đánh giá phỏng vấn |

## 2. Use case chính

```
Quản trị viên
  └── Quản lý tài khoản & phân quyền

HR
  ├── Đăng / sửa / đóng tin tuyển dụng
  ├── Tải lên và quản lý CV ứng viên
  ├── Xem danh sách ứng viên đã được AI xếp hạng
  ├── Phản hồi / hiệu chỉnh điểm AI
  ├── Chuyển trạng thái ứng tuyển
  ├── Xếp lịch phỏng vấn
  └── Xem báo cáo & thống kê

Nhà tuyển dụng
  ├── Xem ứng viên của tin mình phụ trách
  └── Nhập đánh giá sau phỏng vấn
```

## 3. Trạng thái của một lượt ứng tuyển

```
Đã nộp → Sàng lọc → Phỏng vấn → Nhận
                 ↘            ↘
                   Từ chối      Từ chối
```

Quy tắc: chỉ HR được chuyển trạng thái; không được nhảy cóc từ "Đã nộp" thẳng sang "Nhận".

## 4. Cần bổ sung

- [ ] Sơ đồ use case (ảnh hoặc mermaid)
- [ ] Đặc tả chi tiết 3–5 use case quan trọng nhất (luồng chính, luồng phụ, ngoại lệ)
