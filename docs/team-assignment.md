# Phân công nhóm

> Cập nhật tuần 1, điều chỉnh khi cần.

Nhóm chia theo **module ownership** (không phải theo layer). Mỗi người **sở hữu chính** một
mảng nhưng vẫn tham gia review và pair-program ở mảng khác.

## 1. Bảng phân công

| Thành viên | Vai trò chính | Module sở hữu | Layer đảm trách xuyên suốt |
|---|---|---|---|
| **TV1** | Kiến trúc & Data | Cấu trúc solution + Infrastructure chung | SharedKernel, EF DbContext, migration, docker-compose, CI |
| **TV2** | Backend Business | Recruitment + Identity | Domain + Application + API cho 2 module này |
| **TV3** | Frontend | ATS.Web | Blazor Server, UI Ứng viên & HR |
| **TV4** | AI & Test | AiScreening | Toàn bộ module AI + ArchitectureTests + AI integration tests |

## 2. Chi tiết theo tuần

### TV1 — Kiến trúc & Data

- Tuần 1: `architecture.md`, `database-design.md`
- Tuần 2: `.sln` structure, SharedKernel, migration đầu, docker-compose, CI
- Tuần 3–5: EF entity configurations, repositories, `LocalFileStorage`
- Tuần 6: Redis client, ATS.Worker skeleton
- Tuần 7–8: hỗ trợ TV4 với repository của AiScreening
- Tuần 9: query báo cáo, dashboard
- Tuần 10: deploy, seed data, video demo

### TV2 — Backend Business

- Tuần 1: `use-cases.md`, `contracts.md`
- Tuần 2: ATS.Api skeleton, Swagger, auth middleware
- Tuần 3: Identity — Register/Login handlers, JWT
- Tuần 4: Recruitment — JobService, CvService
- Tuần 5: ApplicationService, apply flow
- Tuần 6: API cho AiScreening (screening-jobs, score-preview)
- Tuần 7: hỗ trợ TV4 tích hợp OpenAI adapter
- Tuần 8: Interview + Evaluation handlers
- Tuần 9: hỗ trợ TV3 wiring API-UI
- Tuần 10: bug fix, slide bảo vệ

### TV3 — Frontend

- Tuần 1: wireframe (Figma/excalidraw)
- Tuần 2: Blazor Server skeleton, layout, routing, auth
- Tuần 3: đăng ký/đăng nhập 2 vai
- Tuần 4: UI upload CV, list CV, đăng tin (HR)
- Tuần 5: UI apply — chọn CV
- Tuần 6: UI progress bar sàng lọc, danh sách xếp hạng
- Tuần 7: UI score-preview cho ứng viên
- Tuần 8: UI hẹn phỏng vấn + AI gợi ý câu hỏi
- Tuần 9: dashboard, polish
- Tuần 10: mobile responsive nếu còn thời gian

### TV4 — AI & Test

- Tuần 1: `ai-integration.md`, prompt v1, chi phí
- Tuần 2: ArchitectureTests với NetArchTest
- Tuần 3–4: hỗ trợ review, viết test cho các module khác
- Tuần 5: `SimpleAnonymizer`, unit test PII
- Tuần 6: `FakeAiScoringAdapter`, `IScreeningQueue` interface, ProcessScreeningHandler
- Tuần 7: `OpenAiScoringAdapter`, `EmbeddingScoringAdapter`, `KeywordScoringAdapter`, `AiScoringPipeline`
- Tuần 8: `OpenAiInterviewQuestionAdapter`, `TemplateInterviewQuestionAdapter`
- Tuần 9: snapshot tests, integration tests
- Tuần 10: video demo AI flow, tài liệu prompt

## 3. Quy tắc làm việc

### Ownership

- Mỗi module có 1 owner. PR đụng module đó phải có owner review.
- Owner không có nghĩa "chỉ mình được sửa" — người khác vẫn làm được, chỉ cần owner duyệt.

### Review

- Mỗi PR cần **ít nhất 1 review approval** trước khi merge (thấy `.github/CODEOWNERS`).
- PR đụng ArchitectureTests → TV4 review bắt buộc.
- PR đụng database migration → TV1 review bắt buộc.

### Chuẩn communication

- Daily standup 15 phút mỗi sáng (async trong Discord/Zalo cũng được)
- Weekly sync 1 giờ mỗi chủ nhật để chốt kế hoạch tuần
- Blocker > 4 giờ → tag cả nhóm

### Nếu team thành viên rớt

Contract-first design (mọi interface đóng ở tuần 3) cho phép 3 người vẫn hoàn thành:
- TV1 → gánh Infrastructure chung
- TV2 → gánh Business
- TV3 → gánh UI **hoặc** TV4 gánh AI (chọn 1 để cắt)

Nếu mất TV3: dùng Swagger UI, cắt Blazor.
Nếu mất TV4: giữ Fake adapter, cắt OpenAI real integration (đây là thảm họa nhưng vẫn có kiến trúc).

## 4. Chấm điểm nội bộ

Cuối kỳ, nhóm tự chấm điểm đóng góp theo:
- Số PR merge được
- Số dòng code có ý nghĩa (không tính generated)
- Chất lượng code review nhận được từ người khác
- Tham gia họp

Kết quả tự chấm nộp cho giảng viên trong báo cáo cuối kỳ.
