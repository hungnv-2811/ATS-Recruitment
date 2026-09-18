# ATS.Api

REST API + Composition Root.

## Trách nhiệm

- Wire DI: chọn adapter theo config (`AiProvider: OpenAI | Fake`)
- Controllers: thin, chỉ chuyển tiếp request tới Application layer
- Middleware: auth JWT, error handling, request logging
- Swagger UI

## Không được

- Đụng `AtsDbContext` trực tiếp (dùng Application service)
- Đụng OpenAI SDK trực tiếp (dùng port `IAiScoringService`)
- Chứa logic nghiệp vụ
