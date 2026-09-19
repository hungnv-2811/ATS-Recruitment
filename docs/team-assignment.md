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
- Tuần 2: `.sln` (18 project), SharedKernel + `IEmailSender`, `ATS.Persistence` + migration đầu, docker-compose, CI
- Tuần 3–5: EF entity configurations, repositories, `LocalFileStorage`
- Tuần 6: Redis client, ATS.Worker skeleton
- Tuần 7–8: repository của AiScreening (`ai_score_cache`, `ai_usage_quotas`)
- Tuần 9: trả nợ tuần 7–8 trước; còn thời gian thì query báo cáo + dashboard
- Tuần 10: deploy, seed data, video demo

### TV2 — Backend Business

- Tuần 1: `use-cases.md`, `contracts.md`
- Tuần 2: ATS.Api skeleton, Swagger, auth middleware
- Tuần 3: Identity — Register/Login/ForgotPassword/ResetPassword handlers, JWT
- Tuần 4: Recruitment — JobService, CvService
- Tuần 5: ApplicationService, apply flow
- Tuần 6: API cho AiScreening (screening-jobs, score-preview)
- Tuần 7: hỗ trợ TV4 tích hợp OpenAI adapter
- Tuần 8: Interview + Evaluation handlers, gửi email mời phỏng vấn (ngoài transaction)
- Tuần 9: hỗ trợ TV3 wiring API-UI
- Tuần 10: bug fix, slide bảo vệ

### TV3 — Frontend

- Tuần 1: wireframe (Figma/excalidraw)
- Tuần 2: Blazor Server skeleton, layout, routing, auth
- Tuần 3: đăng ký/đăng nhập 2 vai + màn quên mật khẩu / đặt lại mật khẩu
- Tuần 4: UI upload CV, list CV, đăng tin (HR)
- Tuần 5: UI apply — chọn CV
- Tuần 6: UI progress bar sàng lọc, danh sách xếp hạng
- Tuần 7: UI score-preview cho ứng viên + **nhãn nguồn điểm** (AI / Ngữ nghĩa / Từ khoá)
      + hiển thị số lượt preview còn lại
- Tuần 8: UI hẹn phỏng vấn + AI gợi ý câu hỏi + nút "Gửi lại lời mời"
- Tuần 9: phân quyền UI, dashboard, polish
- Tuần 10: mobile responsive nếu còn thời gian

> **Quy tắc cho TV3:** UI bám theo API **ngay trong tuần API xong**, không để dồn sang tuần 9.
> Một người không thể dựng toàn bộ giao diện của 2 chủ thể trong một tuần — xem
> `weekly-plan.md`, mục "Hai điều chỉnh so với bản nháp đầu".

### TV4 — AI & Test

- Tuần 1: `ai-integration.md`, prompt v1, chi phí
- Tuần 2: ArchitectureTests (5 quy tắc, reflection thuần — xem `architecture.md` mục 3.1)
- Tuần 3–4: hỗ trợ review, viết test cho các module khác
- Tuần 5: `AnonymizedCv` + `IAnonymizer` + `SimpleAnonymizer` (**đặt trong `AiScreening.Domain`**,
      xem ADR-3), unit test PII, ArchitectureTest chặn `Recruitment` chạm `AnonymizedCv`
- Tuần 6: `FakeAiScoringAdapter`, `IScreeningQueue`, `IAiScoreCacheRepository`, `IAiUsageQuota`,
      ProcessScreeningHandler
- Tuần 7: `OpenAiScoringAdapter`, `KeywordScoringAdapter`, `AiScoringPipeline`, cache + quota
      (**Embedding dời sang tuần 9** — nó cắt được, không nên nằm trên đường găng)
- Tuần 8: `OpenAiInterviewQuestionAdapter`, `TemplateInterviewQuestionAdapter`
- Tuần 9: `EmbeddingScoringAdapter` (nếu hết nợ), snapshot tests, integration tests
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
- **Số use case hoàn thành end-to-end** (từ UI xuống DB, có test) — tiêu chí chính
- Số PR merge được
- Chất lượng code review **để lại cho người khác** (bắt được lỗi thật, không phải "LGTM")
- Tham gia họp và giữ đúng cam kết tuần

> **Không dùng "số dòng code" làm tiêu chí.** Nó thưởng cho người viết dài dòng và phạt người
> viết gọn — ngược hẳn tinh thần kiến trúc của đồ án này. Một PR xoá 200 dòng trùng lặp có giá
> trị hơn một PR thêm 200 dòng copy-paste.

Kết quả tự chấm nộp cho giảng viên trong báo cáo cuối kỳ.
