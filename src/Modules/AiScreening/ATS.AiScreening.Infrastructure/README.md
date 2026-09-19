# ATS.AiScreening.Infrastructure

Adapter thật cho ports của module AI.

## Scoring

- `OpenAiScoringAdapter` — tầng 1 (chính)
- `EmbeddingScoringAdapter` — tầng 2 (fallback)
- `KeywordScoringAdapter` — tầng 3 (luôn chạy được)
- `AiScoringPipeline` — chain 3 adapter
- `FakeAiScoringAdapter` — dùng cho test/dev

## Interview questions

- `OpenAiInterviewQuestionAdapter`
- `TemplateInterviewQuestionAdapter` — fallback

## PII (phase 2)

- `LlmPiiRedactor` — implement `IPiiRedactor` (chỉ `string` → `string`)

> Không implement `IAnonymizer` ở đây, và không thêm `InternalsVisibleTo`. Assembly này
> **không được** cầm quyền tạo `AnonymizedCv`. Xem `docs/architecture.md` ADR-3.

## Queue & Repository

- `RedisScreeningQueue`
- `EfAiScoreRepository` — bảng `ai_scores` (khoá theo `application_id`)
- `EfAiScoreCacheRepository` — bảng `ai_score_cache` (khoá theo nội dung, **không** có
  `application_id` — để preview của ứng viên lúc chưa nộp đơn vẫn cache được)
- `EfAiUsageQuota` — bảng `ai_usage_quotas`, chặn hạn mức (RB2)
- `EfScreeningJobRepository`

## Cross-module

- `ScreeningTriggerAdapter` — implement `Recruitment.Application.IApplicationScreeningTrigger`

Xem `docs/ai-integration.md`.
