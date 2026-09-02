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
  ├── **Khởi chạy một lượt sàng lọc AI** cho một tin tuyển dụng   ← chạy nền
  ├── **Theo dõi tiến độ lượt sàng lọc** (đã xong 180/300)
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

## 4. Trạng thái sàng lọc AI — tách riêng khỏi trạng thái ứng tuyển

Hai chuỗi trạng thái này **độc lập với nhau**, và đó là điều bắt buộc: điểm AI là thông tin bổ
trợ, không được tự động đẩy một lượt ứng tuyển đi tiếp (`architecture.md` — RB9).

```
Chưa chấm → Đang chấm → Đã chấm
                     ↘
                       Không chấm được  (LLM lỗi / PDF hỏng)
```

Điều đó có nghĩa là: HR **vẫn xem CV, vẫn chuyển trạng thái ứng tuyển bình thường** khi cột điểm
AI còn trống hoặc ghi *Không chấm được*. Không màn hình nào được chặn thao tác vì thiếu điểm AI.

## 5. Cần bổ sung

- [ ] Sơ đồ use case (ảnh hoặc mermaid)
- [ ] Đặc tả chi tiết 3–5 use case quan trọng nhất (luồng chính, luồng phụ, ngoại lệ)
