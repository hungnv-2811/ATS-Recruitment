# ATS.Identity.Infrastructure

- `EfUserRepository`
- `EfPasswordResetTokenRepository`
- `BCryptPasswordHasher`
- `JwtTokenIssuer` (System.IdentityModel.Tokens.Jwt)

Email đặt lại mật khẩu gửi qua `ATS.SharedKernel.Ports.IEmailSender` — module này không tự
mở kết nối SMTP.
