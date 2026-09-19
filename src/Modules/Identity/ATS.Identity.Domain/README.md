# ATS.Identity.Domain

- **Entities**: `User`, `PasswordResetToken`
- **Value Objects**: `Role` (enum: Candidate/HR/Admin), `Email`
- **Ports**: `IUserRepository`, `IPasswordHasher`, `ITokenIssuer`,
  `IPasswordResetTokenRepository`

Module này chỉ lo **xác thực**. Hồ sơ nghiệp vụ `Candidate` và `HrProfile` thuộc
`Recruitment.Domain` (schema `recruitment`) — xem `docs/database-design.md` mục 2.

`PasswordResetToken` lưu **hash** của token, không lưu token thô; hết hạn 30 phút, dùng
một lần.
