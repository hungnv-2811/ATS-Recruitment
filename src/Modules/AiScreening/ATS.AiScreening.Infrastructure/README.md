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

## Queue & Repository

- `RedisScreeningQueue`
- `EfAiScoreRepository`, `EfScreeningJobRepository`

## Cross-module

- `ScreeningTriggerAdapter` — implement `Recruitment.Application.IApplicationScreeningTrigger`

Xem `docs/ai-integration.md`.
