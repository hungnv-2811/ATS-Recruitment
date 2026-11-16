# Khảo sát

> Tài liệu khảo sát trước khi chốt phạm vi (tuần 1). Gồm ba phần: **hệ thống tương tự** (thị
> trường đã có gì, còn thiếu gì), **người dùng** (ứng viên và HR cần gì), **công nghệ** (vì sao
> chọn bộ công nghệ hiện tại). Mục cuối nối kết quả khảo sát với yêu cầu của hệ thống.

| | |
|---|---|
| Thời gian khảo sát | dd/mm/2026 – dd/mm/2026 *(điền)* |
| Người phụ trách | Hệ thống tương tự: TV1, TV2 · Người dùng: TV3 (ứng viên), TV2 (HR) · Công nghệ: TV1, TV4 |

---

## 1. Khảo sát hệ thống tương tự

### 1.1. Đối tượng khảo sát

| Hệ thống | Loại | Vì sao chọn |
|---|---|---|
| **TopCV** | Cổng việc làm + công cụ tạo CV | Phổ biến nhất với sinh viên Việt Nam |
| **ITviec** | Cổng việc làm chuyên ngành IT | Cùng nhóm người dùng mục tiêu (ứng viên IT) |
| **VietnamWorks** | Cổng việc làm đa ngành | Lâu đời, nhiều doanh nghiệp lớn dùng |
| **LinkedIn Jobs** | Mạng nghề nghiệp + tuyển dụng | Chuẩn tham chiếu quốc tế |
| **Workable / Greenhouse** | Phần mềm ATS cho HR | Đại diện nhóm "hệ thống quản lý tuyển dụng" thuần, phía doanh nghiệp |

Cách khảo sát: dùng thử bằng tài khoản ứng viên (đăng ký, tạo CV, ứng tuyển), đọc trang giới
thiệu dành cho nhà tuyển dụng, và đọc tài liệu công khai của nhà cung cấp. Không dùng được
tài khoản HR trả phí — các mục phía HR dựa trên tài liệu công khai.

> **Lưu ý khi nộp:** tính năng của các nền tảng thay đổi liên tục. Trước khi nộp, người phụ
> trách mở lại từng trang, đối chiếu bảng 1.2 và điền ngày kiểm tra.

### 1.2. So sánh theo tính năng

Ký hiệu: ● có, đầy đủ · ◐ có nhưng hạn chế hoặc chỉ ở gói trả phí · ○ không thấy khi khảo sát.

| Tiêu chí | TopCV | ITviec | VietnamWorks | LinkedIn | ATS (Workable…) | **Hệ thống của nhóm** |
|---|---|---|---|---|---|---|
| Đăng tin, tìm kiếm, lọc việc làm | ● | ● | ● | ● | ◐ (trang tuyển dụng riêng) | ● |
| Ứng viên giữ **nhiều CV**, chọn CV khi nộp | ● | ◐ | ● | ◐ | ○ (phía HR) | ● |
| Ứng viên biết **mức độ phù hợp trước khi nộp** | ◐ | ○ | ◐ | ◐ (gói Premium) | ○ | ● điểm 0–100 + điểm mạnh/còn thiếu |
| HR nhận **danh sách đã xếp hạng** theo độ phù hợp | ◐ | ◐ | ◐ | ◐ | ● | ● sàng lọc hàng loạt chạy nền |
| **Nguồn của điểm** hiển thị rõ (AI hay đếm từ khoá) | ○ | ○ | ○ | ○ | ○ | ● nhãn bắt buộc cạnh mọi điểm |
| AI **gợi ý câu hỏi phỏng vấn** theo CV + JD | ○ | ○ | ○ | ○ | ◐ | ● (HR sửa/xoá được) |
| Hẹn phỏng vấn, gửi lời mời, nhập đánh giá | ◐ | ○ | ◐ | ◐ | ● | ● |
| **Ẩn danh CV trước khi gửi lên AI** (công bố rõ) | ○ | ○ | ○ | ○ | ◐ (blind hiring) | ● ép ở mức kiểu dữ liệu |
| Trạng thái hồ sơ minh bạch cho ứng viên | ◐ | ◐ | ◐ | ◐ | ○ | ● |
| Chi phí cho trường/doanh nghiệp nhỏ | Miễn phí cơ bản | Trả phí đăng tin | Trả phí đăng tin | Trả phí | Trả phí theo tháng | Tự triển khai bằng Docker |

### 1.3. Nhận xét

**Điểm mạnh của thị trường:** cổng việc làm Việt Nam rất mạnh ở phía "chợ việc" — lượng tin,
tìm kiếm, mẫu CV đẹp. Phần mềm ATS quốc tế mạnh ở quy trình phía HR (pipeline, lịch phỏng vấn,
đánh giá nhóm).

**Khoảng trống mà nhóm nhắm vào:**

1. **AI chỉ phục vụ một phía.** Cổng việc làm dùng AI để gợi ý việc cho ứng viên; ATS dùng AI để
   lọc hồ sơ cho HR. Hiếm hệ thống cho **cả hai phía cùng thấy một con số** độ phù hợp.
2. **Điểm AI không rõ nguồn.** Khi AI lỗi và hệ thống rơi về so khớp từ khoá, người dùng vẫn
   thấy một con số giống hệt — dễ so nhầm hai thang điểm khác nhau.
3. **Quyền riêng tư không kiểm chứng được.** Nền tảng thường cam kết bảo vệ dữ liệu bằng chính
   sách, không chỉ ra được cơ chế ngăn CV thô bị gửi tới mô hình ngôn ngữ.
4. **Câu hỏi phỏng vấn soạn tay**, không bám theo CV của từng người.

Bốn khoảng trống này trở thành bốn điểm khác biệt của đồ án (README mục "Điểm khác biệt").
Nhóm **không** cạnh tranh ở phía "chợ việc" (lượng tin, SEO) — đó là thứ đồ án 10 tuần không
làm tốt hơn được.

---

## 2. Khảo sát người dùng

### 2.1. Phương pháp

| | Ứng viên | HR / nhà tuyển dụng |
|---|---|---|
| Hình thức | Phiếu trực tuyến (Google Form) | Phiếu trực tuyến + phỏng vấn ngắn 15 phút |
| Đối tượng | Sinh viên năm 3–4 và người đi làm ≤ 3 năm, ưu tiên ngành IT | HR / trưởng nhóm có tham gia tuyển dụng |
| Cỡ mẫu mục tiêu | ≥ 30 phiếu | ≥ 5 phiếu, phỏng vấn 2–3 người |
| Kênh tiếp cận | Nhóm lớp, câu lạc bộ, mạng xã hội | Người quen ở công ty thực tập, giảng viên hướng dẫn doanh nghiệp |

Phiếu không thu họ tên, email hay số điện thoại — chỉ thu thông tin phục vụ phân tích.

### 2.2. Phiếu khảo sát ứng viên

| # | Câu hỏi | Kiểu trả lời |
|---|---|---|
| U1 | Bạn đang là: sinh viên / mới tốt nghiệp (< 1 năm) / đã đi làm 1–3 năm / khác | Một lựa chọn |
| U2 | Bạn thường tìm việc ở đâu? (TopCV, ITviec, VietnamWorks, LinkedIn, Facebook, khác) | Nhiều lựa chọn |
| U3 | Bạn có giữ nhiều phiên bản CV cho các vị trí khác nhau không? (Không / 2 bản / ≥ 3 bản) | Một lựa chọn |
| U4 | Trước khi nộp, bạn có muốn biết CV phù hợp với tin bao nhiêu % không? | Thang 1–5 |
| U5 | Nếu hệ thống cho biết điểm phù hợp, bạn muốn thấy thêm gì? (Điểm mạnh, kỹ năng còn thiếu, gợi ý sửa CV, so với ứng viên khác) | Nhiều lựa chọn |
| U6 | Bạn có lo ngại khi CV (có họ tên, SĐT, địa chỉ) được gửi cho AI xử lý không? | Thang 1–5 |
| U7 | Nếu hệ thống tự ẩn thông tin cá nhân trước khi gửi AI, bạn có yên tâm hơn không? | Thang 1–5 |
| U8 | Sau khi nộp, bạn có thường biết hồ sơ đang ở bước nào không? (Luôn / Đôi khi / Hầu như không) | Một lựa chọn |
| U9 | Bạn muốn được báo kết quả qua kênh nào? (Email, trên web, Zalo/SMS) | Nhiều lựa chọn |
| U10 | Điều bạn khó chịu nhất khi ứng tuyển trực tuyến là gì? | Trả lời ngắn |

### 2.3. Phiếu khảo sát HR

| # | Câu hỏi | Kiểu trả lời |
|---|---|---|
| H1 | Quy mô công ty (< 50 / 50–200 / 200–1000 / > 1000 người) | Một lựa chọn |
| H2 | Trung bình một tin tuyển dụng nhận bao nhiêu hồ sơ? (< 20 / 20–100 / 100–300 / > 300) | Một lựa chọn |
| H3 | Bạn mất bao lâu để lọc sơ bộ một hồ sơ? (< 1 / 1–3 / 3–5 / > 5 phút) | Một lựa chọn |
| H4 | Công ty đang dùng công cụ gì để quản lý ứng viên? (Excel/Sheet, email, phần mềm ATS, cổng việc làm) | Nhiều lựa chọn |
| H5 | Bạn có tin một điểm phù hợp do AI chấm để xếp hạng hồ sơ không? | Thang 1–5 |
| H6 | Điều kiện để bạn tin điểm đó? (Thấy lý do chấm, biết điểm do AI hay do đếm từ khoá, được xem CV gốc, khác) | Nhiều lựa chọn |
| H7 | Khi AI chưa chấm được một hồ sơ, bạn muốn hệ thống làm gì? (Ẩn hồ sơ / Vẫn hiện ở cuối / Chặn thao tác đến khi chấm xong) | Một lựa chọn |
| H8 | Bạn có tự soạn câu hỏi phỏng vấn riêng cho từng ứng viên không? (Luôn / Đôi khi / Dùng bộ câu cố định) | Một lựa chọn |
| H9 | Một bộ câu hỏi gợi ý từ CV + mô tả công việc có giúp bạn không? | Thang 1–5 |
| H10 | Công ty có quy định gì về việc gửi dữ liệu ứng viên cho dịch vụ AI bên ngoài? | Trả lời ngắn |

Câu hỏi phỏng vấn sâu (HR): mô tả một lần tuyển gần nhất từ lúc đăng tin tới lúc nhận người;
bước nào tốn thời gian nhất; từng bỏ sót ứng viên tốt vì quá nhiều hồ sơ chưa.

### 2.4. Kết quả tổng hợp

> **Điền sau khi đóng phiếu.** Ghi số phiếu thực tế, tỉ lệ cho từng lựa chọn, và đính kèm file
> xuất từ Google Form (`docs/khao-sat/` hoặc link Google Drive). Không điền số ước đoán.

| Chỉ số | Ứng viên | HR |
|---|---|---|
| Số phiếu thu được | … | … |
| Số người phỏng vấn sâu | — | … |

| Câu hỏi then chốt | Kết quả | Ảnh hưởng tới thiết kế |
|---|---|---|
| U3 — giữ nhiều CV | …% giữ từ 2 bản trở lên | Quyết định cho phép **nhiều CV / ứng viên** |
| U4 — muốn biết % phù hợp trước khi nộp | Điểm TB …/5 | Use case UC-05 (xem độ phù hợp) |
| U6, U7 — lo ngại AI đọc CV / yên tâm khi ẩn danh | …/5 → …/5 | Ẩn danh bắt buộc (ADR-3) |
| U8 — biết hồ sơ đang ở bước nào | …% "hầu như không" | Thanh trạng thái cho ứng viên (UV-5) |
| H2, H3 — số hồ sơ × thời gian lọc | … hồ sơ × … phút | Sàng lọc hàng loạt chạy nền (ADR-2) |
| H5, H6 — điều kiện tin điểm AI | …% cần thấy lý do, …% cần biết nguồn | Lý do chấm + **nhãn nguồn điểm** |
| H7 — hồ sơ AI chưa chấm | …% chọn "vẫn hiện ở cuối" | Quy tắc RB9 |
| H9 — câu hỏi gợi ý | Điểm TB …/5 | Use case gợi ý câu hỏi phỏng vấn |
| H10 — quy định gửi dữ liệu cho AI | … | Hạn mức + ẩn danh + chạy được không cần AI |

**Kết luận:** *(2–4 câu, viết sau khi có số liệu: giả thuyết nào được xác nhận, giả thuyết nào
không, nhóm điều chỉnh phạm vi thế nào.)*

---

## 3. Khảo sát công nghệ

Ràng buộc chung: 4 sinh viên, 10 tuần, máy cá nhân 8 GB RAM, ngân sách AI 20–30 USD cả kỳ
(RB1, RB2, RB4 trong [`architecture.md`](architecture.md)).

| Hạng mục | Phương án đã cân nhắc | **Chọn** | Lý do chính |
|---|---|---|---|
| Kiến trúc | Microservices · Monolith một khối · Modular Monolith | **Modular Monolith** + Clean Architecture | Đủ ranh giới module để làm song song và kiểm thử, không tốn công vận hành nhiều service (ADR-1) |
| Backend | Node.js (NestJS) · Java Spring Boot · ASP.NET Core | **ASP.NET Core 10** | Cả nhóm đã học C#; .NET 10 là bản hỗ trợ dài hạn tới 11/2028, .NET 8 hết hỗ trợ đúng lúc kết thúc học phần |
| Cơ sở dữ liệu | SQL Server · MySQL · SQLite · PostgreSQL | **PostgreSQL 16** | Schema tách module, partial unique index (một CV mặc định), image ~80 MB; SQL Server cần ~2 GB RAM riêng |
| Hàng đợi | Gọi AI đồng bộ · Hangfire/Quartz trong tiến trình · RabbitMQ/Kafka · Redis | **Redis** + Worker riêng | Lô 200–300 CV không thể chờ trong một request; Redis nhẹ, lưu xuống đĩa; RabbitMQ/Kafka thừa so với nhu cầu (ADR-2) |
| Giao diện | React/Angular (SPA) · Razor Pages · Blazor Server | **Blazor Server** | Một ngôn ngữ (C#) cho cả nhóm; không cần build JS riêng; dự phòng bằng Swagger UI |
| AI chấm điểm | Chỉ dùng LLM · Chỉ so khớp từ khoá · Nhiều tầng | **OpenAI → Embedding → Từ khoá** | Luôn có điểm kể cả khi mất mạng/hết tiền; mỗi tầng ghi rõ nguồn (ADR-3) |
| Nhà cung cấp LLM | Gemini · LLM chạy máy (Ollama) · OpenAI | **OpenAI `gpt-4o-mini`** | Chi phí thấp, hỗ trợ JSON có cấu trúc; LLM chạy máy vượt RAM; đổi nhà cung cấp chỉ cần thêm adapter |
| Email | Gửi thật từ đầu · MailHog khi dev | **SMTP + MailHog** | Dev không gửi thư ra Internet; đổi sang SMTP thật bằng cấu hình |
| Triển khai | Cài tay từng dịch vụ · Kubernetes · Docker Compose | **Docker Compose** | Một lệnh dựng 6 dịch vụ; giảng viên chạy lại được |
| Kiểm thử | Chỉ unit test · thêm test kiến trúc + PostgreSQL thật | **Unit + ArchitectureTests + Integration (Postgres thật trên CI)** | Ranh giới kiến trúc được kiểm tự động; lỗi SQL/ràng buộc chỉ lộ trên database thật |

Chi tiết từng quyết định và đánh đổi: [`architecture.md`](architecture.md) mục 2–3,
[`ai-integration.md`](ai-integration.md), [`database-design.md`](database-design.md).

---

## 4. Từ khảo sát đến yêu cầu

| Phát hiện | Nguồn | Yêu cầu / quyết định trong hệ thống |
|---|---|---|
| Ứng viên giữ nhiều phiên bản CV | 1.2, U3 | Nhiều CV / ứng viên, chọn CV khi nộp (UC-01, UC-02) |
| Thị trường hiếm cho ứng viên thấy độ phù hợp trước khi nộp | 1.3, U4, U5 | Xem độ phù hợp đồng bộ ≤ 30 giây, kèm điểm mạnh/còn thiếu (UC-05) |
| Điểm AI không rõ nguồn dễ gây so nhầm | 1.3, H6 | Nhãn nguồn điểm bắt buộc cạnh mọi điểm (wireframes mục 0.1) |
| Lo ngại gửi dữ liệu cá nhân cho AI | 1.3, U6, U7, H10 | `AnonymizedCv` — CV phải ẩn danh trước khi tới AI, kiểm bằng ArchitectureTests (ADR-3) |
| HR phải lọc nhiều hồ sơ | H2, H3 | Sàng lọc hàng loạt qua hàng đợi + Worker (ADR-2) |
| Hồ sơ chưa chấm không được biến mất | H7 | Quy tắc RB9: thiếu điểm AI không chặn thao tác nào |
| Câu hỏi phỏng vấn soạn tay, không bám CV | 1.3, H8, H9 | AI gợi ý câu hỏi theo CV + JD, HR sửa/xoá được (UC-04) |
| Ngân sách AI hữu hạn | Ràng buộc RB2 | Cache theo nội dung + hạn mức 20 lượt xem/ngày |
| Ứng viên không biết hồ sơ ở bước nào | U8 | Thanh trạng thái hồ sơ cho ứng viên (UV-5) |
