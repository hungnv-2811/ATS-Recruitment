# ATS.Recruitment.Infrastructure

Adapter cho Recruitment module:

- EF repositories (dùng `AtsDbContext` chung, schema `recruitment`)
- `LocalFileStorage` — lưu CV file vào volume `./data/cvs/`
- `PdfPigTextExtractor`, `OpenXmlTextExtractor`
- `SimpleAnonymizer` (regex)
