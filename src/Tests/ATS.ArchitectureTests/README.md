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
| 1 | Domain chỉ phụ thuộc BCL và SharedKernel | **Allowlist** theo public key token của runtime, duyệt **bắc cầu** |
| 2 | Port AI chỉ nhận `AnonymizedCv`, không nhận `string` | Duyệt `GetParameters()` của từng method trên 2 port |
| 3 | Ngoài Composition Root, không ai chạm `DbContext` | Duyệt ctor, field, property và tham số method của **mọi** type trong `ATS.Api` |
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

## Vì sao quy tắc 1 dùng allowlist chứ không phải denylist

Bản đầu liệt kê 7 chuỗi cấm (`Infrastructure`, `EntityFrameworkCore`, `Npgsql`, …). Cách đó
hụt theo hai hướng: kéo `Dapper`, `MongoDB.Driver` hay `System.Data.SqlClient` vào Domain đều
**xanh** vì không tên nào khớp; và `GetReferencedAssemblies()` chỉ thấy tham chiếu trực tiếp,
nên đường `Domain → SharedKernel → EF Core` lọt qua — đúng cái kịch bản mà `architecture.md`
mục 3.2 loại bỏ bằng lập luận.

Bản hiện tại đảo lại: chỉ cho phép assembly của .NET runtime (nhận diện theo **public key
token**, vì BCL có những assembly không mang tiền tố `System.` như `Microsoft.Win32.Primitives`,
và ngược lại package bên thứ ba hoàn toàn có thể tự đặt tên `System.Something`), cộng
`ATS.SharedKernel` và chính nó. Duyệt toàn bộ bao đóng tham chiếu. Package lạ thêm vào ngày mai
tự động bị chặn mà không ai phải nhớ cập nhật danh sách.

## Giới hạn đã biết của quy tắc 3

API dùng Minimal API, nên nếu handler viết thẳng thành lambda trong `Program.cs` thì nó nằm
trong closure của Composition Root và được miễn trừ. Chặn được cả trường hợp đó thì phải quét
IL. Từ tuần 3, handler phải nằm trong class riêng để quy tắc này có hiệu lực thật.
