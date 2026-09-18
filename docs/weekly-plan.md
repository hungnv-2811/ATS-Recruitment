# Kế hoạch 10 tuần

> Cả team cập nhật hàng tuần trong `docs/worklog/`.

Mục tiêu cuối: demo được luồng end-to-end **Ứng viên upload CV → nộp → AI chấm → HR sàng lọc →
hẹn phỏng vấn với AI gợi ý câu hỏi → nhập đánh giá**. Kiến trúc Clean + Modular Monolith +
Ports & Adapters có ArchitectureTests xanh.

## Tổng quan

| Tuần | Mốc | Deliverable |
|---|---|---|
| 1–2 | Khung kiến trúc | `.sln` + 15 project, docker-compose up được, ArchitectureTests xanh |
| 3–4 | Identity + Recruitment (Job, Cv) | Đăng ký/đăng nhập 2 vai, HR CRUD tin, Ứng viên upload CV |
| 5 | Ứng tuyển + Anonymizer | Ứng viên nộp CV vào tin, SimpleAnonymizer chạy |
| 6 | AiScreening với FakeAdapter | Enqueue → Worker → Fake trả điểm ngẫu nhiên, luồng end-to-end xanh |
| 7 | AI Adapter thật (Scoring) | OpenAI + Embedding + Keyword, pipeline fallback |
| 8 | AI Adapter thật (Questions) + Phỏng vấn | Sinh câu hỏi, xếp lịch, đánh giá |
| 9 | Web UI + Báo cáo | Blazor Server cho Ứng viên & HR, dashboard đơn giản |
| 10 | Polish + Deploy + Demo | Fix bug, tối ưu UX, chuẩn bị bảo vệ |

---

## Tuần 1: Chốt phạm vi & thiết kế

**Mục tiêu:** Tất cả tài liệu thiết kế đóng, team đồng ý về ranh giới module & API.

- [ ] TV1: Hoàn thiện `architecture.md`, `database-design.md`
- [ ] TV2: Hoàn thiện `use-cases.md`, `contracts.md`
- [ ] TV3: Wireframe Blazor cho Ứng viên & HR (Figma hoặc excalidraw)
- [ ] TV4: Hoàn thiện `ai-integration.md` (prompt v1, fallback strategy)
- [ ] Cả team: review lẫn nhau, đóng ADR

**Không code trong tuần 1.** Đây là quy tắc — code trước khi thiết kế xong sẽ phải rewrite.

**Rủi ro:** Giảng viên yêu cầu sửa scope → dự phòng 1 buổi họp cuối tuần 1.

---

## Tuần 2: Dựng khung

**Mục tiêu:** Team clone repo, chạy `docker compose up`, thấy API xanh, ArchitectureTests pass.

- [ ] TV1: Tạo `ATS.sln`, 15 project, project references chuẩn
  - [ ] `ATS.SharedKernel` với `Entity`, `ValueObject`, `Result<T>`
  - [ ] Migration đầu tiên (chỉ tạo schema `identity`, `recruitment`, `aiscreening`)
- [ ] TV2: `ATS.Api` skeleton: chương trình chính, Swagger, auth middleware (chưa validate)
- [ ] TV3: `ATS.Web` Blazor Server skeleton, page trắng, layout, routing
- [ ] TV4: `ATS.ArchitectureTests` với 3 rule bắt buộc
  - [ ] Domain không reference Infrastructure
  - [ ] Port AI chỉ nhận `AnonymizedCv`
  - [ ] Controller không đụng DbContext
- [ ] Cả team: `docker-compose.yml` với 5 service (db, queue, api, worker, web)
- [ ] Cả team: CI xanh (build + test)

**Deliverable:** `git clone → docker compose up → curl localhost:5000/health → 200 OK`.

---

## Tuần 3: Identity module

**Mục tiêu:** Đăng ký/đăng nhập 2 vai (Candidate, HR) hoạt động.

- [ ] Domain: `User`, `Role` (enum: Candidate/HR/Admin), `IUserRepository`, `IPasswordHasher`, `ITokenIssuer`
- [ ] Application: `RegisterHandler`, `LoginHandler`
- [ ] Infrastructure: EF `UsersConfig`, `BCryptPasswordHasher`, `JwtTokenIssuer`
- [ ] API: `POST /api/auth/register`, `POST /api/auth/login`
- [ ] Test: unit test cho `RegisterHandler` (mock repo)

**Ai làm chính:** TV2 (Application + API), TV1 (Domain + EF), TV4 (test).

**Deliverable:** đăng ký ứng viên → đăng nhập → nhận JWT → gọi `/api/me` được.

---

## Tuần 4: Recruitment — Job + Cv

**Mục tiêu:** HR đăng tin, Ứng viên upload nhiều CV.

- [ ] Domain: `Job`, `Candidate`, `Cv` (relation 1-N), `IJobRepository`, `ICvRepository`, `IFileStorage`
- [ ] Application: `JobService` (CRUD), `CvService` (upload, list, delete, set default)
- [ ] Infrastructure: EF repositories, `LocalFileStorage` (lưu vào volume `./data/cvs/`)
- [ ] API: `POST/GET/PATCH /api/jobs`, `POST/GET/DELETE /api/me/cvs`
- [ ] Text extraction: `PdfPigTextExtractor`, `OpenXmlTextExtractor` (chạy async sau upload)

**Ai làm chính:** TV1 (Domain + EF), TV2 (API + Application), TV3 bắt đầu UI upload CV.

**Deliverable:** ứng viên upload 2 CV, HR đăng 1 tin, cả hai xem được.

---

## Tuần 5: Application + Anonymizer

**Mục tiêu:** Ứng viên nộp CV vào tin (chọn 1 trong các CV), có `SimpleAnonymizer` chạy.

- [ ] Domain: `Application` (state machine), `IApplicationRepository`
- [ ] Domain (AiScreening): `AnonymizedCv` value object, `IAnonymizer` port
- [ ] Infrastructure: `SimpleAnonymizer` (regex)
- [ ] Application: `ApplyHandler` (validate CV thuộc candidate, tạo Application)
- [ ] Application: `IApplicationScreeningTrigger` (chưa dùng thật, chỉ enqueue dummy)
- [ ] API: `POST /api/applications`, `GET /api/me/applications`
- [ ] UI: màn hình chọn CV + apply

**Ai làm chính:** TV2 (nghiệp vụ ứng tuyển), TV4 (Anonymizer + test), TV3 (UI).

**Deliverable:** ứng viên nộp CV vào tin, thấy trong lịch sử ứng tuyển.

---

## Tuần 6: AiScreening với FakeAdapter (end-to-end xanh)

**Mục tiêu:** Toàn bộ luồng AI đi từ enqueue → Worker → điểm lưu vào DB, dùng Fake để **không tốn tiền**.

- [ ] Domain: `IAiScoringService`, `IScreeningQueue`, `AiScore`, `ScreeningJob`, `CacheKey`
- [ ] Application: `StartScreeningHandler`, `ProcessScreeningHandler`, `PreviewScoreHandler` (dùng cho ứng viên)
- [ ] Infrastructure: `FakeAiScoringAdapter` (trả điểm random 40-95), `RedisScreeningQueue`
- [ ] ATS.Worker host: background service đọc queue, gọi handler
- [ ] API: `POST /api/screening-jobs`, `GET /api/screening-jobs/{id}`, `POST /api/ai/score-preview`
- [ ] Cross-module wiring: `ScreeningTriggerAdapter` implement `IApplicationScreeningTrigger`

**Ai làm chính:** TV4 (AiScreening module), TV1 (Redis + Worker), TV2 (API), TV3 (UI progress bar).

**Deliverable:** upload CV → apply → 3 giây sau thấy điểm 78 kèm summary giả. **Không có 1 dòng OpenAI SDK nào chạy — dùng Fake.**

---

## Tuần 7: AI Adapter thật — Scoring

**Mục tiêu:** Thay Fake bằng pipeline thật 3 tầng.

- [ ] Infrastructure: `OpenAiScoringAdapter` (OpenAI SDK, retry, timeout, structured output)
- [ ] Infrastructure: `EmbeddingScoringAdapter` (text-embedding-3-small, cosine)
- [ ] Infrastructure: `KeywordScoringAdapter` (đếm keyword match)
- [ ] Infrastructure: `AiScoringPipeline` — chain 3 adapter
- [ ] Cache: check `ai_scores.cache_key` trước khi gọi LLM
- [ ] Config: `AiProvider: OpenAI | Fake` để switch qua config
- [ ] Snapshot test: 5 cặp (CV, JD) mẫu

**Ai làm chính:** TV4 (chính), TV2 hỗ trợ.

**Deliverable:** demo được cả 3 adapter: OpenAI (chạy đủ), Embedding (khi disable OpenAI qua config), Keyword (khi disable cả hai).

---

## Tuần 8: AI Interview Questions + Phỏng vấn

**Mục tiêu:** HR hẹn phỏng vấn → AI sinh câu hỏi → nhập đánh giá.

- [ ] Domain: `IInterviewQuestionGenerator`, `InterviewQuestion`, `Interview`, `Evaluation`
- [ ] Infrastructure: `OpenAiInterviewQuestionAdapter`, `TemplateInterviewQuestionAdapter` (fallback)
- [ ] Application: `GenerateQuestionsHandler`, `ScheduleInterviewHandler`, `SubmitEvaluationHandler`
- [ ] API: `POST /api/applications/{id}/questions`, `POST /api/applications/{id}/interviews`, `POST /api/interviews/{id}/evaluations`
- [ ] UI: màn hẹn phỏng vấn với nút "Gợi ý câu hỏi", màn đánh giá

**Ai làm chính:** TV4 (AI part), TV2 (nghiệp vụ), TV3 (UI).

**Deliverable:** HR mở 1 hồ sơ → bấm "Hẹn phỏng vấn" → bấm "Gợi ý câu hỏi" → thấy 10 câu bám sát CV+JD → lưu lại.

---

## Tuần 9: Web UI hoàn thiện + Báo cáo

**Mục tiêu:** UI dùng được, không cần Swagger để demo.

- [ ] UI cho Ứng viên: đăng nhập, quản lý CV, xem tin, apply với preview điểm, lịch sử
- [ ] UI cho HR: đăng nhập, quản lý tin, xem ứng viên xếp hạng, chi tiết hồ sơ, phỏng vấn, đánh giá
- [ ] Báo cáo: dashboard đơn giản (số hồ sơ theo trạng thái, top tin nhiều ứng viên, chi phí AI)
- [ ] Phân quyền UI: page Ứng viên không hiện được cho HR và ngược lại
- [ ] Responsive: chạy được trên mobile (chỉ cần đọc, không cần thao tác đẹp)

**Ai làm chính:** TV3 (UI), TV1 (báo cáo query), TV2 hỗ trợ.

**Deliverable:** demo hoàn chỉnh không cần mở Swagger.

---

## Tuần 10: Polish + Deploy + Demo

**Mục tiêu:** Sản phẩm ổn định, bảo vệ được.

- [ ] Fix bugs từ tuần 9
- [ ] Seed data đầy đủ cho demo (5 HR, 20 ứng viên, 15 tin, 60 application)
- [ ] Docker build tất cả image, đẩy lên Docker Hub (private) — để giảng viên chạy được
- [ ] README + hướng dẫn chạy
- [ ] Slide bảo vệ + kịch bản demo (10 phút)
- [ ] Video demo backup (phòng khi mạng lỗi)
- [ ] Bản báo cáo cuối kỳ (Word)

**Ai làm chính:** cả team.

**Deliverable:** buổi bảo vệ.

---

## Fallback nếu chậm tiến độ

Cắt theo thứ tự sau (đã sắp xếp theo mức độ ảnh hưởng thấp → cao):

1. **Báo cáo/dashboard** (tuần 9) → thay bằng vài query SQL đơn giản, hiện trong Swagger
2. **UI Blazor đẹp** (tuần 9) → dùng Swagger UI + form đơn giản để demo
3. **AI Interview Questions** (tuần 8) → giữ Template fallback, bỏ OpenAI adapter cho câu hỏi
4. **Embedding adapter** (tuần 7) → chỉ giữ OpenAI + Keyword
5. **Mobile responsive** → chỉ desktop

**Tuyệt đối không cắt:**
- `AnonymizedCv` value object
- ArchitectureTests
- Fallback 3 tầng cho scoring
- Redis queue + Worker (đây là điểm sáng kiến trúc)
- Ứng viên có nhiều CV
- 2 chủ thể (HR + Ứng viên) — cắt cái này thì đề tài mất bản sắc

## Rủi ro chính

| Rủi ro | Xác suất | Ảnh hưởng | Ứng phó |
|---|---|---|---|
| OpenAI hết quota giữa kỳ | Trung bình | Cao | Fallback 3 tầng đã có, thay $10 nữa |
| PDF text extraction lỗi cho CV thiết kế lạ | Cao | Trung bình | Đánh dấu "không đọc được", vẫn cho apply |
| Anonymizer regex sót thông tin | Cao | Cao (PII) | Prompt hệ thống có câu chốt + review 20 mẫu bằng tay |
| Team thành viên rớt | Thấp | Cao | Contract-first + module tách rõ → 3 người vẫn làm được |
| Blazor Server lag | Trung bình | Thấp | Fallback Swagger UI |
