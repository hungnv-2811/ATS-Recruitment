# ATS.Api

REST API + Composition Root.

## Trách nhiệm

- Wire DI: chọn adapter theo config (`AiProvider: OpenAI | Fake`)
- Controllers: thin, chỉ chuyển tiếp request tới Application layer
- Middleware: auth JWT, error handling, request logging
- Swagger UI

## Về việc API có gọi LLM hay không

**Có, đúng một chỗ:** `POST /api/ai/score-preview` — ứng viên xem % phù hợp của 1 CV trước
khi nộp (UC-05). Gọi **đồng bộ**, timeout cứng 30s, có rate-limit 20 lượt/ngày. Đây là ngoại
lệ có chủ đích của ADR-2, được ghi rõ trong `docs/architecture.md`.

Sàng lọc **hàng loạt** thì không: API chỉ enqueue rồi trả `202`, `ATS.Worker` mới gọi LLM.

Hệ quả: container `api` **cũng cần** `OPENAI_API_KEY`, và `AiProvider` của `api` phải đặt
**giống** `worker`.

## Không được

- Đụng `AtsDbContext` trực tiếp (dùng Application service)
- Đụng OpenAI SDK trực tiếp — kể cả ở `score-preview`. Luôn đi qua port `IAiScoringService`;
  adapter nào được tiêm vào là việc của Composition Root
- Chứa logic nghiệp vụ
