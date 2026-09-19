# ATS.SharedKernel

Primitive dùng chung cho toàn solution: `Entity<TId>`, `Result<T>`, `Error`, `IUnitOfWork`.

**KHÔNG chứa nghiệp vụ.** Đây là chỗ đặt những abstraction ở tầng thấp nhất mà mọi module có thể dùng mà không tạo phụ thuộc nghiệp vụ.

## Nội dung dự kiến

- `Entity<TId>` — base class có `Id`, `Equals`, `GetHashCode` theo Id

> **Không có `ValueObject` base class.** Value object của dự án dùng `record` — compiler
> đã sinh sẵn so sánh theo giá trị, `GetHashCode` và toán tử `==`. Một base class viết tay
> chỉ tạo ra hai quy ước song song để người viết code tuần sau phải chọn giữa chúng.
- `Result<T>` — success/failure không cần exception
- `IUnitOfWork` — commit atomically. `AtsDbContext` implement interface này
- `Ports/IEmailSender` + `EmailMessage` — cả `Identity` (đặt lại mật khẩu) lẫn `Recruitment`
  (mời phỏng vấn) đều cần gửi mail. Đây là hạ tầng kỹ thuật, không mang nghiệp vụ, nên đặt ở
  đây thay vì để một module phải reference module kia. Xem `docs/contracts.md` mục 2.4
