# ATS.Recruitment.Infrastructure

Adapter cho Recruitment module:

- EF repositories (dùng `AtsDbContext` chung, schema `recruitment`)
- `LocalFileStorage` — lưu CV file vào volume `./data/cvs/`
- `PdfPigTextExtractor`, `OpenXmlTextExtractor`
- `SmtpEmailSender` / `NullEmailSender` — implement `ATS.SharedKernel.Ports.IEmailSender`

**KHÔNG có ở đây:** `SimpleAnonymizer`. Nó nằm trong `AiScreening.Domain` — xem
`docs/architecture.md` ADR-3.
