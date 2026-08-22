# Tích hợp AI

> TV4 phụ trách tài liệu này. Bản nháp nộp tuần 2, hoàn chỉnh tuần 7.

## 1. Sáu câu hỏi bắt buộc

| Câu hỏi | Trả lời |
|---|---|
| **Ai sử dụng?** | Nhân viên tuyển dụng (HR) và nhà tuyển dụng |
| **Giải quyết vấn đề gì?** | Đọc và so khớp hàng trăm CV thủ công tốn thời gian, dễ bỏ sót ứng viên phù hợp, thiếu nhất quán |
| **Dữ liệu đầu vào?** | Nội dung CV (PDF/text) và mô tả công việc (JD) |
| **Kết quả đầu ra?** | Bản tóm tắt CV + điểm phù hợp 0–100 + danh sách ứng viên xếp hạng kèm lý do |
| **Dùng trong nghiệp vụ nào?** | Bước **sàng lọc hồ sơ** trước khi mời phỏng vấn |
| **Nếu AI sai thì sao?** | HR luôn thấy CV gốc; điểm AI chỉ để tham khảo/sắp xếp; quyết định cuối do con người; có nút phản hồi để hiệu chỉnh và ghi log |

## 2. Mức tích hợp

**Mức 1** — dùng dịch vụ AI có sẵn:

- **Embedding**: `text-embedding-3-small` để so khớp ngữ nghĩa CV–JD qua cosine similarity
- **LLM**: `gpt-4o-mini` để tóm tắt CV và sinh lý do phù hợp

Có thể mở rộng lên **Mức 2** nếu tiến độ cho phép: hiệu chỉnh một mô hình phân loại độ phù hợp
trên dữ liệu CV đã gán nhãn.

## 3. Pipeline 7 bước

| Bước | Việc | Kỹ thuật |
|---|---|---|
| 1 | Trích xuất text từ CV | PdfPig |
| 2 | Ẩn danh dữ liệu nhạy cảm | Regex + rule (CCCD, SĐT, email, địa chỉ) |
| 3 | Rút trường có cấu trúc | LLM hoặc rule (kỹ năng, kinh nghiệm, học vấn) |
| 4 | Sinh vector embedding | `text-embedding-3-small` |
| 5 | Tính độ tương đồng CV × JD | Cosine similarity → chuẩn hoá về 0–100 |
| 6 | Sinh tóm tắt + lý do | `gpt-4o-mini` |
| 7 | Xếp hạng ứng viên | Sắp xếp theo điểm |

**Vì sao kết hợp embedding + LLM chứ không dùng riêng LLM:** embedding cho điểm số ổn định,
rẻ và có thể so sánh giữa các ứng viên; LLM giải thích được lý do nhưng đắt và không nhất quán
nếu dùng để chấm điểm. Kết hợp cho cả hai: điểm khách quan + lý do dễ hiểu.

## 4. Prompt

### Tóm tắt CV

```
(dán system prompt + user prompt tại đây, kèm giải thích vì sao viết như vậy)
```

### Sinh lý do phù hợp

```
(dán prompt tại đây)
```

**Chống hallucination:** yêu cầu mô hình chỉ dùng thông tin có trong CV, trả về "không rõ" khi
thiếu dữ liệu, và cấm suy diễn kinh nghiệm không được nêu.

## 5. Fallback khi AI lỗi

| Tình huống | Xử lý |
|---|---|
| API timeout | Retry 2 lần, backoff luỹ tiến |
| API trả 429 / 503 | Xếp hàng chờ, hiển thị "đang chấm điểm" |
| API sập kéo dài | Degrade về tìm kiếm từ khoá; toàn bộ chức năng ATS còn lại vẫn chạy |
| Kết quả vô lý | HR bấm nút phản hồi, hệ thống ghi log để rà lại prompt |

**Nguyên tắc:** không chức năng nghiệp vụ nào của ATS bị chặn khi dịch vụ AI lỗi.

## 6. Bảo mật dữ liệu

- Ẩn danh CCCD, số điện thoại, email **trước khi** gửi tới dịch vụ AI.
- Không gửi file CV gốc lên API bên ngoài, chỉ gửi text đã làm sạch.
- API key lưu trong user-secrets / biến môi trường, không commit.

## 7. Ước tính chi phí

| Hạng mục | Đơn giá | Số lượng dự kiến | Thành tiền |
|---|---|---|---|
| Embedding | — | — | — |
| LLM tóm tắt | — | — | — |
| **Tổng cho toàn dự án** | | | **—** |

> TV4 điền sau khi đo trên demo cốt lõi.

## 8. Kết quả thử nghiệm

| CV mẫu | JD | Điểm AI | Nhận xét của nhóm |
|---|---|---|---|
| CV-01 | | | |
| CV-02 | | | |
| CV-03 | | | |
