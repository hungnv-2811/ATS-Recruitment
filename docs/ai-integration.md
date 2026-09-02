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

Fallback **ba cấp**, khớp với ADR-02 trong `architecture.md`:

| Cấp | Khi nào | Hệ thống làm gì | HR thấy gì |
|---|---|---|---|
| **1** | Bình thường | Embedding + cosine → điểm; LLM → tóm tắt + lý do | Điểm + tóm tắt đầy đủ |
| **2** | LLM lỗi/hết credit | **Vẫn chấm điểm** bằng embedding + cosine, ghi `SummaryStatus = Unavailable` | Vẫn có điểm và xếp hạng, chỉ thiếu tóm tắt |
| **3** | Cả embedding cũng hỏng | Lùi về so khớp từ khoá, đánh dấu độ tin cậy thấp | Gợi ý thô kèm cảnh báo |

Xử lý theo loại lỗi:

| Tình huống | Xử lý |
|---|---|
| Timeout, 429, 5xx (lỗi **tạm thời**) | Retry tối đa **3 lần**, backoff luỹ thừa + jitter |
| PDF hỏng, không trích được text (lỗi **vĩnh viễn**) | **Không retry** — vào dead-letter kèm lý do. Thử lại chỉ tốn tiền |
| API sập kéo dài | Đổi `AI_PROVIDER` sang adapter khác bằng biến môi trường, không sửa code |
| Kết quả vô lý | HR bấm nút phản hồi, hệ thống ghi log kèm `PromptVersion` để rà lại |

**Hai nguyên tắc:**

1. Không chức năng nghiệp vụ nào của ATS bị chặn khi dịch vụ AI lỗi — HR vẫn mở CV gốc, vẫn
   chuyển trạng thái ứng tuyển bình thường.
2. Vì việc chạy nền qua hàng đợi, lỗi của một CV **không làm hỏng cả lô**: CV đó vào dead-letter,
   299 CV còn lại vẫn chạy tiếp.

## 6. Bảo mật dữ liệu

- Ẩn danh CCCD, số điện thoại, email **trước khi** gửi tới dịch vụ AI.
- Không gửi file CV gốc lên API bên ngoài, chỉ gửi text đã làm sạch.
- **Việc ẩn danh được cưỡng chế bằng hệ thống kiểu, không bằng kỷ luật:** `IAiScoringService`
  nhận `AnonymizedCv` chứ không nhận `string`, nên gọi bằng CV thô là **lỗi biên dịch**. Đây là
  điểm khác biệt giữa "có quy định phải ẩn danh" và "không thể quên ẩn danh".
- API key chỉ cấu hình cho **worker** (`ATS.AI`) — `ATS.Api` không bao giờ gọi thẳng LLM nên
  không cần khoá. Lưu trong user-secrets / biến môi trường, không commit.

## 7. Ước tính chi phí

Chi phí tỉ lệ thuận với **số lần gọi**, nên phải ước trước chứ không đợi hết credit mới biết.

**Khối lượng token cho một lượt chấm một CV:**

| Thành phần | Token |
|---|---|
| Nội dung CV sau trích xuất và rút gọn | ~1.500 vào |
| Mô tả công việc (JD) | ~500 vào |
| Tóm tắt + lý do sinh ra | ~300 ra |
| **Tổng mỗi lượt** | **~2.000 vào / ~300 ra** |

**Suy ra cho cả học phần** (tính cả chạy thử, gỡ prompt, demo lặp): ~3.000 lượt chấm →
**~6 triệu token vào, ~0,9 triệu token ra**. Với đơn giá `p_in`, `p_out` (USD/1 triệu token),
chi phí ≈ `6 × p_in + 0,9 × p_out` USD. **Trần ngân sách nhóm tự đặt: 20–30 USD.**

| Hạng mục | Đơn giá thực tế | Số lượng đo được | Thành tiền |
|---|---|---|---|
| Embedding | — | — | — |
| LLM tóm tắt | — | — | — |
| **Tổng cho toàn dự án** | | | **—** |

> TV4 điền cột "thực tế" sau khi đo trên demo cốt lõi tuần 2, rồi so với ước tính ở trên.

**Ba cơ chế chặn đốt tiền** (bắt buộc, xem ADR-02):

1. **Cache theo khoá bất biến** `hash(CV) + hash(JD) + PromptVersion + ModelVersion` — chấm lại
   cùng một cặp thì đọc cache, không gọi API. Đây là cơ chế tiết kiệm lớn nhất, vì phần lớn chi
   phí thật của đồ án đến từ chạy lại khi gỡ lỗi và khi tập demo.
2. **Retry có giới hạn**, không retry lỗi vĩnh viễn.
3. **Hạn mức số lượt gọi mỗi ngày**, và ước tính chi phí hiển thị cho HR trước khi chạy một lô.

## 8. Kết quả thử nghiệm

| CV mẫu | JD | Điểm AI | Nhận xét của nhóm |
|---|---|---|---|
| CV-01 | | | |
| CV-02 | | | |
| CV-03 | | | |
