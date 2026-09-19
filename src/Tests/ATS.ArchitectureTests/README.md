# ATS.ArchitectureTests

Ép ranh giới kiến trúc. Vi phạm → CI đỏ → không merge được.

Đây là chỗ duy nhất biến câu "đồ án này có Clean Architecture" từ một lời hứa trong báo cáo
thành một thứ **kiểm chứng được**.

## Cài đặt: reflection thuần, không thư viện ngoài

Bản nháp đầu định dùng NetArchTest. Bỏ vì **ba trong năm quy tắc dưới đây không diễn đạt được
bằng thư viện đó** — quy tắc 2 phải đọc chữ ký method, quy tắc 5 phải đọc attribute ở mức
assembly. `System.Reflection` đủ sức diễn đạt cả năm và bớt được một dependency.

## Năm quy tắc

| # | Quy tắc | Kiểm bằng cách |
|---|---|---|
| 1 | Domain không phụ thuộc Infrastructure | `Assembly.GetReferencedAssemblies()` của 4 assembly Domain |
| 2 | Port AI chỉ nhận `AnonymizedCv`, không nhận `string` | Duyệt `GetParameters()` của từng method trên 2 port |
| 3 | Controller không inject `DbContext` | Duyệt constructor + field của mọi type `*Controller` trong `ATS.Api` |
| 4 | `Recruitment` không tham chiếu `AiScreening` | Tên assembly được tham chiếu |
| 5 | `AnonymizedCv` không tạo được từ ngoài | Không có ctor `public`, và **không có `InternalsVisibleTo`** |

Quy tắc 1 chạy trên 4 assembly và quy tắc 2 chạy trên 2 port, nên tổng cộng **9 test**.

## Vì sao quy tắc 5 tồn tại

Đây là quy tắc dễ bị phá ngầm nhất. Chỉ cần một dòng:

```csharp
[assembly: InternalsVisibleTo("ATS.AiScreening.Infrastructure")]
```

là toàn bộ bảo đảm compile-time của RB3 biến mất — mà **code vẫn biên dịch, mọi test khác vẫn
xanh, code review rất dễ cho qua** vì nó trông như một dòng kỹ thuật vô hại. Quy tắc 5 là thứ
duy nhất bắt được.

Cần anonymizer bằng LLM thì dùng `IPiiRedactor` (chỉ nhận/trả `string`), đừng implement
`IAnonymizer` ở Infrastructure. Xem `docs/architecture.md` ADR-3.

## Các test này đã được chứng minh là không vô dụng

Một bộ test kiến trúc chưa bao giờ đỏ là một bộ test chưa được chứng minh. Đã kiểm bằng cách
cố tình phá luật:

| Phá gì | Kết quả |
|---|---|
| Thêm `string rawCvText` vào `IAiScoringService.ScoreAsync` | Quy tắc 2 **đỏ** |
| Thêm `[assembly: InternalsVisibleTo(...)]` vào `AiScreening.Domain` | Quy tắc 5 **đỏ** |
| (cả hai cùng lúc) | Đúng 2 test đỏ, 7 test còn lại vẫn xanh |

Nên làm lại phép thử này mỗi khi thêm quy tắc mới.

## Lưu ý về quy tắc 3

Tuần 2 chưa có controller nào nên phép kiểm này **còn rỗng** — nó pass mà chưa kiểm gì cả.
Bắt đầu có tác dụng từ tuần 3, khi `ATS.Api` có controller đầu tiên.
