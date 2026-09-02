# ATS.Tests

xUnit: unit test, integration test, E2E. **Chủ sở hữu: TV4.**

Ngoài test chức năng, project này còn giữ **test ranh giới kiến trúc** (`NetArchTest.Rules`) —
thứ biến các quy ước trong `docs/architecture.md` thành điều kiện biên dịch/CI thay vì lời hứa:

| Test | Bảo vệ điều gì |
|---|---|
| Module không phụ thuộc trực tiếp vào nhau | Ranh giới module của ADR-01 |
| Tầng nghiệp vụ không biết SDK hạ tầng (`OpenAI`, `Npgsql`…) | ADR-03 |
| `ATS.Api` không tham chiếu `ATS.AI` | ADR-02 — API không bao giờ gọi thẳng LLM |

Toàn bộ test phải chạy được **không cần mạng và không cần API key**: dùng `FakeScoringAdapter`
cho phần AI. Chất lượng mô hình được đánh giá riêng bằng **golden set** 10–20 cặp CV–JD đã gán
nhãn, chạy tay theo tuần, chấm bằng ngưỡng thống kê — không so khớp chuỗi chính xác.

> Thư mục còn trống — project .NET sẽ được tạo ở tuần 2.
