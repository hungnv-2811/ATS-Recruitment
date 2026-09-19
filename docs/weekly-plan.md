# Kế hoạch 10 tuần

> Cả team cập nhật hàng tuần trong `docs/worklog/`.

Mục tiêu cuối: demo được luồng end-to-end **Ứng viên upload CV → nộp → AI chấm → HR sàng lọc →
hẹn phỏng vấn với AI gợi ý câu hỏi → nhập đánh giá**. Kiến trúc Clean + Modular Monolith +
Ports & Adapters có ArchitectureTests xanh.

## Tổng quan

| Tuần | Mốc | Deliverable |
|---|---|---|
| 1–2 | Khung kiến trúc | `.sln` + **18 project**, docker-compose up được, ArchitectureTests xanh |
| 3–4 | Identity + Recruitment (Job, Cv) | Đăng ký/đăng nhập 2 vai + quên mật khẩu, HR CRUD tin, Ứng viên upload nhiều CV |
| 5 | Ứng tuyển + Anonymizer | Ứng viên nộp CV vào tin; `SimpleAnonymizer` chạy **trong `AiScreening.Domain`** |
| 6 | AiScreening với FakeAdapter | Enqueue → Worker → Fake trả điểm, luồng end-to-end xanh, cache + quota có bảng |
| 7 | AI Adapter thật: **OpenAI + Keyword** | Pipeline 2 tầng chạy thật, `ai_score_cache` hoạt động, preview đồng bộ |
| 8 | Phỏng vấn + sinh câu hỏi + email | Hẹn phỏng vấn, AI gợi ý câu hỏi, MailHog nhận được mail |
| 9 | **Tuần đệm** + Embedding + báo cáo | Bù phần trượt của tuần 7–8; nếu không trượt thì thêm `EmbeddingScoringAdapter` + dashboard |
| 10 | Polish + Deploy + Demo | Fix bug, seed, slide, video backup |

### Hai điều chỉnh so với bản nháp đầu, và lý do

**1. UI không dồn vào tuần 9.** Bản cũ đặt "toàn bộ UI cho cả 2 chủ thể + dashboard + phân
quyền + responsive" vào một tuần cho một người — việc đó không xảy ra được. UI phải bám theo
API **ngay trong tuần API xong** (tuần 3 làm UI đăng nhập, tuần 4 làm UI upload CV, …), đúng
như `team-assignment.md` mục 2 đã ghi. Tuần 9 chỉ còn hoàn thiện và dashboard.

**2. Tuần 9 là tuần đệm có chủ đích.** Bản cũ xếp tuần 7 và tuần 8 cạnh nhau, cả hai đều là
"adapter AI thật", không có khoảng trống nào ở giữa. Tuần 7 trượt là tuần 8 sập theo dây
chuyền, và hỏng luôn buổi demo. `EmbeddingScoringAdapter` bị đẩy sang tuần 9 vì nó đã nằm ở
vị trí **số 4** trong danh sách cắt giảm dưới đây — thứ cắt được thì không nên nằm trên đường
găng.

---

## Tuần 1: Chốt phạm vi & thiết kế — ✅ XONG

**Mục tiêu:** Tất cả tài liệu thiết kế đóng, team đồng ý về ranh giới module & API.

- [x] TV1: Hoàn thiện `architecture.md`, `database-design.md`
- [x] TV2: Hoàn thiện `use-cases.md`, `contracts.md`
- [x] TV3: Wireframe cho Ứng viên & HR → [`wireframes.md`](wireframes.md)
- [x] TV4: Hoàn thiện `ai-integration.md` (prompt v1, fallback strategy)
- [x] Cả team: review chéo, đóng ADR

### Kết quả của buổi review chéo

Buổi review chéo bắt được **6 mâu thuẫn giữa các tài liệu** — tất cả đều nằm ở ranh giới giữa
hai tài liệu do hai người khác nhau viết, đúng loại lỗi mà bước này sinh ra để bắt:

| Lỗi | Đã chốt thế nào |
|---|---|
| `AnonymizedCv` được đặt ở 3 chỗ khác nhau, và ctor `internal` sẽ **không biên dịch được** qua ranh giới assembly | Về `AiScreening.Domain`, kèm `SimpleAnonymizer` (ADR-3) |
| UC-05 nói preview đồng bộ, compose lại ghi "API không bao giờ gọi LLM" | Ngoại lệ có chủ đích của ADR-2, `api` cũng cần khoá |
| Cache preview không có chỗ lưu (`ai_scores.application_id` là FK bắt buộc) | Tách bảng `ai_score_cache` |
| Index xếp hạng `(application_id, score DESC)` vô dụng | Covering index + `LEFT JOIN … NULLS LAST` |
| Ba chức năng đã hứa nhưng không có thiết kế | Bổ sung đủ: quên mật khẩu, email, rate-limit |
| `candidates` nằm ở schema `identity` hay `recruitment` | `recruitment` (module sở hữu entity) |

**Bài học ghi lại cho các tuần sau:** mỗi người đọc tài liệu của người khác, không phải đọc
lại tài liệu của mình.

**Không code trong tuần 1.** Đây là quy tắc — code trước khi thiết kế xong sẽ phải rewrite.

**Rủi ro:** Giảng viên yêu cầu sửa scope → dự phòng 1 buổi họp cuối tuần 1.

---

## Tuần 2: Dựng khung — ✅ XONG

**Mục tiêu:** Team clone repo, chạy `docker compose up`, thấy API xanh, ArchitectureTests pass.

- [x] TV1: Tạo `ATS.sln`, **18 project**, project references chuẩn
      (1 SharedKernel + 1 Persistence + 3 module × 3 layer + 3 host + 4 test project)
  - [x] `ATS.SharedKernel`: `Entity<TId>`, `ValueObject`, `Result<T>`, `Error`, `IUnitOfWork`,
        `Ports/IEmailSender`
  - [x] `ATS.Persistence`: `AtsDbContext` + `AtsDbContextConfigurator` + design-time factory
  - [x] Migration `20260919131605_InitialCreate` — tạo 3 schema, chưa có bảng
  - [x] `Directory.Build.props`: `TargetFramework` khai báo một chỗ cho cả 18 project
  - [x] `dotnet-tools.json`: ghim version `dotnet-ef` cho cả nhóm
- [x] TV2: `ATS.Api` skeleton — Swagger, JWT middleware (cấu hình sẵn, chưa endpoint nào
      `[Authorize]`), `GET /health`
- [x] TV3: `ATS.Web` Blazor Server skeleton — layout, routing, dark mode, `HttpClient` trỏ
      `ApiBaseUrl` (Web **không** reference project module nào)
- [x] TV4: `ATS.ArchitectureTests` — **5 quy tắc / 9 test**
  - [x] 1. Domain không reference Infrastructure
  - [x] 2. Port AI chỉ nhận `AnonymizedCv`
  - [x] 3. Controller không đụng `DbContext`
  - [x] 4. `Recruitment` không tham chiếu `AiScreening`
  - [x] 5. `AnonymizedCv` không có ctor public **và** không có `InternalsVisibleTo`
  - [x] Kiểm chứng bằng cách cố tình phá luật → đúng 2 test đỏ, 7 test xanh
- [x] Cả team: `docker-compose.yml` với 6 service (db, queue, api, worker, web, mailhog)
- [x] Cả team: CI xanh (build + test)

### Quyết định phát sinh trong tuần 2

**1. Dùng .NET 10 thay vì .NET 8.** Học phần kết thúc cuối tháng 11/2026 — đúng lúc .NET 8 hết
hạn hỗ trợ. .NET 10 là LTS tới 11/2028. Xem `architecture.md` mục 3.5.

**2. Thêm project thứ 18 `ATS.Persistence`.** Một `AtsDbContext` dùng chung cần một chỗ ở
chung: đặt trong Infrastructure của một module thì hai module kia phải reference chéo; đặt
trong `SharedKernel` thì Domain gián tiếp phụ thuộc EF Core. Xem `architecture.md` mục 3.2.

**3. ArchitectureTests dùng reflection thuần thay vì NetArchTest.** Ba trong năm quy tắc không
diễn đạt được bằng thư viện đó. Bớt một dependency.

**4. Bật `TreatWarningsAsErrors`.** Rẻ khi áp từ tuần 2, đắt khi áp vào tuần 8.

**Deliverable — đã kiểm chứng:**

```
git clone
cp docker/.env.example docker/.env
docker compose -f docker/docker-compose.yml up -d --build
curl localhost:8080/health          →  200 OK
                                        {"status":"healthy","aiProvider":"Fake",...}
psql -c "\dn"                        →  identity, recruitment, aiscreening
dotnet build                         →  0 warning, 0 error
dotnet test                          →  26/26 xanh
```

Migration tự chạy lúc API khởi động (`Database__AutoMigrate`), nên `up -d` xong là database
đã sẵn 3 schema — không phải chạy thêm lệnh nào.

**Hai lỗi bắt được lúc kiểm chứng, đã sửa:**

1. **Cổng host hardcode trong compose.** Máy có dự án khác chiếm 8080 thì `curl` trả về trang
   đăng nhập của ứng dụng đó — triệu chứng đánh lừa hơn hẳn "connection refused", vì **có**
   thứ đang lắng nghe, chỉ không phải API của mình. Đã cho đổi cổng qua `API_PORT` trong
   `docker/.env`.
2. **`Directory.Packages.props` không được copy vào image.** Build local xanh nhưng build
   Docker đỏ: không có file version, NuGet tụt về bản thấp nhất và kéo `npgsql 3.1.5` có lỗ
   hổng bảo mật. Đã thêm vào lệnh `COPY` của Dockerfile.

---

## Tuần 3: Identity module

**Mục tiêu:** Đăng ký/đăng nhập 2 vai (Candidate, HR) hoạt động.

- [ ] Domain: `User`, `Role` (enum: Candidate/HR/Admin), `IUserRepository`, `IPasswordHasher`,
      `ITokenIssuer`, `PasswordResetToken`, `IPasswordResetTokenRepository`
- [ ] Application: `RegisterHandler`, `LoginHandler`, `ForgotPasswordHandler`, `ResetPasswordHandler`
- [ ] Infrastructure: EF `UsersConfig`, `BCryptPasswordHasher`, `JwtTokenIssuer`
- [ ] SharedKernel + Infrastructure: `IEmailSender` → `SmtpEmailSender` (MailHog) + `NullEmailSender`
- [ ] API: `POST /api/auth/register`, `/login`, `/forgot-password`, `/reset-password`
- [ ] Test: unit test `RegisterHandler` (mock repo); test token hết hạn + token dùng lại lần 2
- [ ] UI: màn đăng ký / đăng nhập / quên mật khẩu cho cả 2 vai (TV3 — làm ngay tuần này)

**Ai làm chính:** TV2 (Application + API), TV1 (Domain + EF), TV3 (UI), TV4 (test).

**Deliverable:** đăng ký ứng viên → đăng nhập → nhận JWT → gọi `/api/me` được. Quên mật khẩu →
mở MailHog ở `localhost:8025` thấy mail → đổi được mật khẩu.

> **Lưu ý bảo mật (dễ bị hỏi khi bảo vệ):** `/forgot-password` **luôn** trả `204`, kể cả khi
> email không tồn tại. Trả `404` cho email lạ nghĩa là biếu không kẻ tấn công công cụ dò xem
> địa chỉ nào đã đăng ký. Bảng lưu **hash** của token, không lưu token thô.

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

- [ ] Domain (Recruitment): `Application` (state machine), `IApplicationRepository`
- [ ] Domain (AiScreening): `AnonymizedCv` (**ctor `internal`**), `IAnonymizer`, `IPiiRedactor`
- [ ] **Domain (AiScreening)**: `SimpleAnonymizer` (regex) — đặt trong **Domain**, KHÔNG phải
      Infrastructure. Đây là điều kiện để `internal` ctor hoạt động mà không cần
      `InternalsVisibleTo`; xem `architecture.md` ADR-3
- [ ] ArchitectureTest: khẳng định `Recruitment.*` **không** tham chiếu `AnonymizedCv`
- [ ] Application: `ApplyHandler` (validate CV thuộc candidate, tạo Application)
- [ ] Application: `IApplicationScreeningTrigger` (chưa dùng thật, chỉ enqueue dummy)
- [ ] API: `POST /api/applications`, `GET /api/me/applications`
- [ ] UI: màn hình chọn CV + apply

**Ai làm chính:** TV2 (nghiệp vụ ứng tuyển), TV4 (Anonymizer + test), TV3 (UI).

**Deliverable:** ứng viên nộp CV vào tin, thấy trong lịch sử ứng tuyển.

---

## Tuần 6: AiScreening với FakeAdapter (end-to-end xanh)

**Mục tiêu:** Toàn bộ luồng AI đi từ enqueue → Worker → điểm lưu vào DB, dùng Fake để **không tốn tiền**.

- [ ] Domain: `IAiScoringService`, `IScreeningQueue`, `AiScore`, `ScreeningJob`, `CacheKey`,
      `IAiScoreCacheRepository`, `IAiUsageQuota`
- [ ] Infrastructure: `EfAiScoreCacheRepository` (bảng `ai_score_cache` — **tách khỏi**
      `ai_scores`, không có `application_id`), `EfAiUsageQuota` (bảng `ai_usage_quotas`)
- [ ] Application: `StartScreeningHandler`, `ProcessScreeningHandler`, `PreviewScoreHandler` (dùng cho ứng viên)
- [ ] Infrastructure: `FakeAiScoringAdapter` (trả điểm random 40-95), `RedisScreeningQueue`
- [ ] ATS.Worker host: background service đọc queue, gọi handler
- [ ] API: `POST /api/screening-jobs`, `GET /api/screening-jobs/{id}`,
      `POST /api/ai/score-preview` (**đồng bộ**, timeout 30s), `GET /api/me/ai-quota`
- [ ] Cross-module wiring: `ScreeningTriggerAdapter` implement `IApplicationScreeningTrigger`

**Ai làm chính:** TV4 (AiScreening module), TV1 (Redis + Worker), TV2 (API), TV3 (UI progress bar).

**Deliverable:** upload CV → apply → 3 giây sau thấy điểm 78 kèm summary giả. **Không có 1 dòng OpenAI SDK nào chạy — dùng Fake.**

---

## Tuần 7: AI Adapter thật — Scoring (2 tầng)

**Mục tiêu:** Thay Fake bằng pipeline thật. **Tầng 1 và tầng 3 trước** — hai tầng này đã đủ
bảo đảm "luôn có điểm". `EmbeddingScoringAdapter` (tầng 2) đẩy sang tuần 9 vì nó là thứ **cắt
được** (vị trí số 4 trong danh sách cắt giảm), không nên nằm trên đường găng.

- [ ] Infrastructure: `OpenAiScoringAdapter` (OpenAI SDK, retry, timeout, structured output)
- [ ] Infrastructure: `KeywordScoringAdapter` (đếm keyword match — **luôn chạy được**)
- [ ] Infrastructure: `AiScoringPipeline` — chain adapter, tầng trước fail thì rơi xuống tầng sau
- [ ] Cache: tra `ai_score_cache` theo `cache_key` **trước** khi gọi LLM; ghi lại sau khi chấm
- [ ] Quota: `IAiUsageQuota` chặn ở `score-preview`, trả `429` khi hết lượt.
      **Trúng cache không trừ lượt**
- [ ] Config: `AiProvider: OpenAI | Fake`. Kiểm tra **`api` và `worker` đặt giống nhau** —
      lệch nhau thì ứng viên nhận điểm giả còn HR nhận điểm thật
- [ ] UI: nhãn nguồn điểm (AI / Ngữ nghĩa / Từ khoá) cạnh mọi chỗ hiện điểm
- [ ] Snapshot test: 5 cặp (CV, JD) mẫu

**Ai làm chính:** TV4 (chính), TV2 hỗ trợ, TV3 (nhãn nguồn điểm trên UI).

**Deliverable:** demo được fallback thật — tắt OpenAI qua config thì Keyword tiếp quản, điểm
vẫn hiện, nhãn đổi thành "Từ khoá". Bấm preview lần 2 cùng CV+JD → trúng cache, không tốn tiền,
không trừ lượt.

---

## Tuần 8: AI Interview Questions + Phỏng vấn

**Mục tiêu:** HR hẹn phỏng vấn → AI sinh câu hỏi → nhập đánh giá.

- [ ] Domain: `IInterviewQuestionGenerator`, `InterviewQuestion`, `Interview`, `Evaluation`
- [ ] Infrastructure: `OpenAiInterviewQuestionAdapter`, `TemplateInterviewQuestionAdapter` (fallback)
- [ ] Application: `GenerateQuestionsHandler`, `ScheduleInterviewHandler`, `SubmitEvaluationHandler`
- [ ] **Email mời phỏng vấn** qua `IEmailSender` — gửi **sau khi** đã lưu `Interview`, **ngoài**
      transaction. SMTP lỗi thì buổi phỏng vấn vẫn còn, đánh dấu `email_sent = false`, HR có
      nút "Gửi lại lời mời"
- [ ] API: `POST /api/applications/{id}/questions`, `POST /api/applications/{id}/interviews`, `POST /api/interviews/{id}/evaluations`
- [ ] UI: màn hẹn phỏng vấn với nút "Gợi ý câu hỏi", màn đánh giá, nút gửi lại lời mời

**Ai làm chính:** TV4 (AI part), TV2 (nghiệp vụ + email), TV3 (UI).

**Deliverable:** HR mở 1 hồ sơ → bấm "Hẹn phỏng vấn" → bấm "Gợi ý câu hỏi" → thấy 10 câu bám
sát CV+JD → lưu lại → mở `localhost:8025` (MailHog) thấy lời mời. Tắt MailHog rồi làm lại:
buổi phỏng vấn **vẫn phải được lưu**.

---

## Tuần 9: Tuần đệm + Embedding + Báo cáo

**Mục tiêu:** đóng mọi thứ còn dở của tuần 7–8. Chỉ khi **không còn nợ** mới làm phần thêm.

Đầu tuần họp 30 phút, trả lời đúng một câu: *"tuần 7 và 8 có món nào chưa xong?"*

**Ưu tiên 1 — trả nợ (luôn làm trước):**

- [ ] Đóng nốt việc trượt của tuần 7–8
- [ ] Phân quyền UI: page Ứng viên không mở được bằng tài khoản HR và ngược lại
- [ ] Rà soát: mọi chỗ hiện điểm AI đều có nhãn nguồn

**Ưu tiên 2 — chỉ khi hết nợ:**

- [ ] `EmbeddingScoringAdapter` (text-embedding-3-small, cosine) chèn vào giữa pipeline
- [ ] Báo cáo: dashboard (số hồ sơ theo trạng thái, top tin nhiều ứng viên, **tỉ lệ cache hit**,
      **chi phí AI tích luỹ**)
- [ ] Responsive mobile (chỉ cần đọc được)

**Ai làm chính:** cả team trả nợ; TV1 (báo cáo query), TV4 (Embedding), TV3 (polish).

**Deliverable:** demo hoàn chỉnh không cần mở Swagger. Nếu tuần 7–8 đúng hạn thì có thêm
pipeline đủ 3 tầng và dashboard.

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
4. **Embedding adapter** (đã dời sang tuần 9) → chỉ giữ OpenAI + Keyword
5. **Mobile responsive** → chỉ desktop
6. **Email mời phỏng vấn** → hiện thông báo trong hệ thống thay cho gửi mail

**Tuyệt đối không cắt:**
- `AnonymizedCv` value object
- ArchitectureTests
- Fallback 3 tầng cho scoring
- Redis queue + Worker (đây là điểm sáng kiến trúc)
- Ứng viên có nhiều CV
- 2 chủ thể (HR + Ứng viên) — cắt cái này thì đề tài mất bản sắc
- `ai_score_cache` tách khỏi `ai_scores` — gộp lại thì preview của ứng viên không cache được,
  RB2 mất chỗ dựa
- Nhãn nguồn điểm (`AdapterUsed`) hiện cạnh mọi điểm AI — giấu đi là để HR so nhầm hai thang
  điểm khác nhau

## Rủi ro chính

| Rủi ro | Xác suất | Ảnh hưởng | Ứng phó |
|---|---|---|---|
| OpenAI hết quota giữa kỳ | Trung bình | Cao | Fallback 3 tầng đã có, thay $10 nữa |
| PDF text extraction lỗi cho CV thiết kế lạ | Cao | Trung bình | Đánh dấu "không đọc được", vẫn cho apply |
| Anonymizer regex sót thông tin | Cao | Cao (PII) | Prompt hệ thống có câu chốt + review 20 mẫu bằng tay |
| Team thành viên rớt | Thấp | Cao | Contract-first + module tách rõ → 3 người vẫn làm được |
| Blazor Server lag | Trung bình | Thấp | Fallback Swagger UI |
| Tuần 7 trượt (adapter thật nhiều việc hơn ước lượng) | Cao | Cao | Tuần 9 là tuần đệm; Embedding đã dời khỏi đường găng |
| `api` và `worker` cấu hình `AiProvider` lệch nhau | Trung bình | Cao (ứng viên thấy điểm giả mà không biết) | Ghi cùng một giá trị trong compose; thêm log cảnh báo lúc khởi động khi hai bên lệch |
| UI dồn cuối kỳ | Cao | Cao | UI bám theo API ngay trong tuần API xong, không để dồn |
