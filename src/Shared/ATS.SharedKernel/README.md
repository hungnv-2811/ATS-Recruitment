# ATS.SharedKernel

Primitive dùng chung cho toàn solution: `Entity<TId>`, `ValueObject`, `Result<T>`, `IUnitOfWork`.

**KHÔNG chứa nghiệp vụ.** Đây là chỗ đặt những abstraction ở tầng thấp nhất mà mọi module có thể dùng mà không tạo phụ thuộc nghiệp vụ.

## Nội dung dự kiến

- `Entity<TId>` — base class có `Id`, `Equals`, `GetHashCode` theo Id
- `ValueObject` — base class so sánh theo giá trị
- `Result<T>` — success/failure không cần exception
- `IUnitOfWork` — commit atomically
- `IDomainEvent` — event trong domain (dùng nếu cần sau)
- `Ports/IEmailSender` + `EmailMessage` — cả `Identity` (đặt lại mật khẩu) lẫn `Recruitment`
  (mời phỏng vấn) đều cần gửi mail. Đây là hạ tầng kỹ thuật, không mang nghiệp vụ, nên đặt ở
  đây thay vì để một module phải reference module kia. Xem `docs/contracts.md` mục 2.4
