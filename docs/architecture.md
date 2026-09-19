# Kiến trúc hệ thống

> TV1 phụ trách tài liệu này. Cập nhật trong tuần 1–2.
> Tài liệu này đồng thời là **chương Kiến trúc** của báo cáo học phần.

Chương này trình bày kiến trúc theo bốn tầng quyết định: **(1)** bối cảnh và ràng buộc buộc phải
chấp nhận, **(2)** ba quyết định kiến trúc cùng phương án thay thế đã cân nhắc và lý do loại bỏ,
**(3)** cấu trúc code cụ thể (thư mục, project, ranh giới), **(4)** những đánh đổi nhóm chấp
nhận trả giá và dấu hiệu phải xem lại quyết định.

Nguyên tắc viết chương: **không mô tả công nghệ, mà giải thích lựa chọn**. Một sơ đồ chỉ có giá
trị khi trả lời được câu hỏi "vì sao không phải cách khác".

---

## 1. Bối cảnh và ràng buộc

### 1.1. Vấn đề nghiệp vụ

Hệ thống phục vụ **hai chủ thể** cùng lúc:

- **Ứng viên** cần một chỗ để quản lý nhiều CV, tìm tin phù hợp, và biết *trước khi nộp* hồ sơ
  của mình hợp bao nhiêu phần trăm với tin đó — thay vì gửi mù rồi chờ.
- **HR** cần một chỗ nhận hồ sơ đã được xếp hạng sẵn, chuyển trạng thái theo quy trình chuẩn,
  và khi hẹn phỏng vấn thì có ngay một bộ câu hỏi bám sát JD + CV của ứng viên.

Ở giữa là **AI trợ lý hai chiều** — thứ tạo giá trị khác biệt so với các cổng tuyển dụng chỉ
đóng vai trò "chợ" (đăng tin + nộp CV).

### 1.2. Quy mô và tải dự kiến

| Chỉ số | Ước lượng | Ghi chú |
|---|---|---|
| Ứng viên đăng ký sau 1 năm | 500 – 2.000 | Không phải quy mô công cộng lớn |
| HR có tài khoản | 5 – 20 | Người dùng nội bộ |
| Ứng viên đồng thời cao điểm | 20 – 50 | Sau giờ hành chính |
| Tin tuyển dụng đang mở | 10 – 30 | |
| CV upload mỗi tháng | ~ 500 | Đỉnh tải: khi HR chạy sàng lọc lô 200–300 hồ sơ |
| Số CV mỗi ứng viên | 1 – 5 | Trung bình 2 |

**Đặc điểm tải quan trọng:** *thấp về CRUD, cao và giật ở AI screening*. Đây là cái quyết định
kiến trúc phải lưu ý.

### 1.3. Ràng buộc (mã `RB*` để tham chiếu)

| Mã | Ràng buộc | Ảnh hưởng |
|---|---|---|
| RB1 | 4 sinh viên, 10 tuần | Không thể ôm microservices, event-driven phức tạp |
| RB2 | Ngân sách AI ~ $20–30 cho cả kỳ | Bắt buộc cache + fallback |
| RB3 | Dữ liệu PII (CV có tên, SĐT, email, địa chỉ) | Ẩn danh trước khi ra khỏi hệ thống |
| RB4 | Chạy được trên máy sinh viên (8GB RAM) | Không dùng infra nặng |
| RB5 | Phải deploy được (docker compose) | Simple deploy |
| RB6 | Học phần: chấm cả tư duy thiết kế | Kiến trúc phải giải thích được |
| RB7 | Có thể thay LLM provider giữa chừng | Phải abstract hóa LLM |
| RB8 | Ứng viên chỉ được thấy dữ liệu của chính mình | Phân quyền chặt ở tầng ứng dụng |
| RB9 | Điểm AI là *gợi ý*, không tự chuyển trạng thái ứng tuyển | Tách chuỗi trạng thái AI khỏi chuỗi trạng thái nghiệp vụ |

---

## 2. Ba quyết định kiến trúc (ADR)

### ADR-1: **Modular Monolith, không phải microservices**

**Bối cảnh:** hệ thống có 3 bounded context rõ (Recruitment, AiScreening, Identity). Với 3
context, mô hình microservices được nhắc đến nhiều nhất trong tài liệu học thuật.

**Quyết định:** một solution duy nhất, chia thành 3 module logic, deploy 3 host process
(API, Worker, Web) dùng chung database, giao tiếp cross-module qua interface trong quá trình.

**Phương án đã cân nhắc và loại bỏ:**

| Phương án | Vì sao loại |
|---|---|
| Microservices (3 service riêng) | RB1: 4 người 10 tuần không đủ để làm service discovery, message bus, distributed trace |
| Monolith đơn khối (không tách module) | Sau 3 tháng code sẽ rối, khó review, khó test — RB6 không đạt |
| Monorepo microservices | Cùng nhược điểm với microservices, thêm phức tạp CI |

**Đánh đổi chấp nhận:** một lỗi trong module `AiScreening` có thể kéo cả API xuống nếu không
tách process. → Giải quyết bằng cách tách **Worker** thành process riêng: API chỉ enqueue
việc AI, Worker mới thực sự gọi LLM. Nếu Worker chết thì API vẫn phục vụ CRUD.

**Dấu hiệu phải xem lại:** khi có > 5 module, hoặc khi một module cần scale độc lập theo cách
khác biệt hẳn (ví dụ AiScreening cần 10 replica còn Recruitment 1 replica).

### ADR-2: **Sàng lọc AI chạy bất đồng bộ qua Queue + Worker**

**Bối cảnh:** một lượt gọi LLM tốn 5–30 giây. HR upload 300 CV thì HTTP request không thể chờ.

**Quyết định:** API nhận request, ghi `ScreeningJob` vào DB, đẩy message vào Redis queue, trả
`202 Accepted` kèm `jobId`. Worker riêng đọc queue, gọi LLM, ghi kết quả. Client polling
`GET /screening-jobs/{id}`.

**Phương án đã cân nhắc và loại bỏ:**

| Phương án | Vì sao loại |
|---|---|
| Gọi LLM đồng bộ trong request | Timeout, UX tệ, không xử lý được lô lớn |
| Rabbit/Kafka | RB1, RB4: nặng máy, thừa tính năng |
| Chỉ dùng Hangfire/Quartz (in-process job) | Không tách được scale của Worker; nếu API restart thì job pending mất — Redis persistent |
| Server-Sent Events / WebSocket cho tiến độ | Đẹp hơn nhưng RB1: tốn công, polling `GET /screening-jobs/{id}` đủ dùng |

**Đánh đổi chấp nhận:** thêm 1 dependency (Redis) và 1 process (Worker) → tăng độ phức tạp
deploy. → Bù lại bằng docker-compose gom sẵn.

**Ngoại lệ có chủ đích — `POST /api/ai/score-preview` gọi LLM đồng bộ ngay trong API.**

Quyết định bất đồng bộ ở trên áp dụng cho **sàng lọc lô** (HR chạy 200–300 hồ sơ). Riêng use
case "ứng viên xem % phù hợp trước khi nộp" (UC-05) chỉ là **một** cặp CV↔JD, và toàn bộ giá
trị của nó nằm ở chỗ trả lời ngay trên màn hình ứng viên đang đứng. Đẩy một lượt gọi duy nhất
qua queue nghĩa là thêm bảng preview, thêm endpoint polling, thêm vòng lặp UI — ba thứ để
phục vụ đúng một lần gọi 5–30 giây.

| | Sàng lọc lô (HR) | Preview (Ứng viên) |
|---|---|---|
| Số lượt gọi LLM | 200–300 | 1 |
| Đường đi | API → Redis → Worker | API gọi thẳng adapter |
| Phản hồi | `202 Accepted` + polling | `200 OK` đồng bộ |
| Timeout | không (Worker tự retry) | 30 giây cứng |
| Khi quá hạn | job `Failed`, HR chạy lại | rơi xuống Keyword adapter, vẫn có điểm |

**Hệ quả bắt buộc phải nhất quán trong cấu hình:** process `api` **cũng cần**
`OPENAI_API_KEY`, và `AiProvider` của `api` phải đặt **giống** `worker`. Nếu để `api` chạy
`Fake` còn `worker` chạy `OpenAI` thì ứng viên sẽ luôn nhận điểm giả trong khi HR nhận điểm
thật — xem phần ghi chú trong `docker/docker-compose.yml`.

Hai hàng rào giữ cho ngoại lệ này không lan rộng: **(1)** timeout cứng 30 giây, **(2)**
rate-limit 20 lượt/ngày mỗi ứng viên, lưu ở bảng `aiscreening.ai_usage_quotas`
(xem [`ai-integration.md`](ai-integration.md) mục 4).

**Dấu hiệu phải xem lại:** khi latency polling gây khó chịu → chuyển sang SSE ở phase sau.
Hoặc khi p95 của preview vượt 30 giây, hoặc có > 50 ứng viên preview đồng thời → lúc đó mới
đẩy preview qua queue như sàng lọc lô.

### ADR-3: **LLM (và mọi AI use-case) đứng sau Port trong Domain**

**Bối cảnh:** hệ thống có **2 năng lực AI riêng biệt**:

1. **Chấm độ phù hợp CV ↔ JD** (dùng bởi cả HR và Ứng viên)
2. **Sinh câu hỏi phỏng vấn** (dùng bởi HR khi hẹn phỏng vấn)

Nếu code gọi thẳng SDK OpenAI ở Application, ta không test được (mỗi test tốn tiền), không
thay được provider, không fallback được khi LLM lỗi (RB2, RB7).

**Quyết định:** định nghĩa **hai port riêng biệt** trong `AiScreening.Domain`:

```csharp
public interface IAiScoringService
{
    Task<ScreeningResult> ScoreAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct);
}

public interface IInterviewQuestionGenerator
{
    Task<IReadOnlyList<InterviewQuestion>> GenerateAsync(
        AnonymizedCv cv, JobRequirement jd, CancellationToken ct);
}
```

Cả hai port đều nhận `AnonymizedCv` — **kiểu dữ liệu riêng, không phải `string`**. Compiler ép
mọi CV phải qua bước ẩn danh trước khi gửi lên LLM. Đây là cách hiện thực RB3 ở compile-time
thay vì trông chờ vào kỷ luật lập trình.

**`AnonymizedCv`, `IAnonymizer` và bản hiện thực regex đều nằm trong `AiScreening.Domain`.**
Đây không phải chi tiết tuỳ tiện — nó là điều kiện để cơ chế trên hoạt động thật:

- Constructor của `AnonymizedCv` là `internal` → chỉ code **cùng assembly** mới tạo được.
- `SimpleAnonymizer` (regex thuần, không I/O, không SDK ngoài) đặt luôn trong Domain → tạo
  được `AnonymizedCv` mà **không cần** `InternalsVisibleTo`. Không có lỗ hổng nào phải nới ra.
- Module `Recruitment` **không** tham chiếu `AnonymizedCv`. Nó chỉ gửi `applicationId` qua
  `IApplicationScreeningTrigger`; việc đọc CV và ẩn danh diễn ra hoàn toàn bên trong
  `AiScreening`. Nhờ vậy quy tắc "module không reference chéo" ở mục 3.3 vẫn đúng.

Khi cần anonymizer mạnh hơn bằng LLM (phase 2), **không** implement `IAnonymizer` ở
Infrastructure — làm vậy buộc phải mở `internal` ra và mất luôn bảo đảm compile-time. Thay vào
đó thêm một port cấp thấp hơn, chỉ làm việc trên `string`:

```csharp
// AiScreening.Domain — adapter bên Infrastructure implement port này
public interface IPiiRedactor
{
    Task<string> RedactAsync(string rawCvText, CancellationToken ct);
}
```

`SimpleAnonymizer` trong Domain gọi `IPiiRedactor` (nếu được cấu hình), rồi mới bọc kết quả
thành `AnonymizedCv`. Infrastructure xử lý được text nhưng **không bao giờ** cầm được quyền
tạo `AnonymizedCv`.

Adapter thật sống trong `AiScreening.Infrastructure`:

- `OpenAiScoringAdapter` (chính) → gọi OpenAI, có retry
- `EmbeddingScoringAdapter` (fallback 2) → chỉ dùng embedding + cosine similarity
- `KeywordScoringAdapter` (fallback 3) → so khớp keyword từ JD với CV, luôn chạy được không cần LLM
- `AiScoringPipeline` → chain 3 adapter theo thứ tự, adapter trước fail thì rơi xuống adapter sau
- `OpenAiInterviewQuestionAdapter` + `TemplateInterviewQuestionAdapter` (fallback từ template có sẵn)

**Phương án đã cân nhắc và loại bỏ:**

| Phương án | Vì sao loại |
|---|---|
| Gọi SDK OpenAI trực tiếp trong Application | Không test được, không fallback được — vi phạm RB2, RB7 |
| Một port chung `IAiService` với `type: string` | Mất type safety, prompt lẫn với logic, khó maintain |
| Gói prompt vào Domain | Prompt là chi tiết hạ tầng (thay đổi theo model version) — thuộc Infrastructure |

**Đánh đổi chấp nhận:** cần duy trì FakeAdapter cho unit test, và fallback logic phải tự viết
(không có framework sẵn). → Đây chính là cái được chấm điểm trong bảo vệ — chứng minh Ports &
Adapters giải quyết vấn đề thực.

**Dấu hiệu phải xem lại:** khi có > 4 port AI → có thể cần một pipeline framework nhẹ hơn.

---

## 3. Cấu trúc code

Solution `ATS.sln` chia làm **4 vùng**: `Shared`, `Modules`, `Hosts`, `Tests`.

```
src/
├── Shared/
│   ├── ATS.SharedKernel/                # KHÔNG chứa nghiệp vụ, chỉ primitive
│   │                                      # Entity, ValueObject, Result<T>, Error,
│   │                                      # IUnitOfWork, Ports/IEmailSender
│   └── ATS.Persistence/                 # AtsDbContext + Migrations (xem 3.2)
├── Modules/
│   ├── Recruitment/                     # Nghiệp vụ tuyển dụng cốt lõi
│   │   ├── ATS.Recruitment.Domain/      #   Job, Candidate, Cv (1-N), Application, Interview,
│   │   │                                #   IJobRepository, ICvRepository, IFileStorage, ...
│   │   ├── ATS.Recruitment.Application/ #   Service class + validator (không CQRS đầy đủ)
│   │   └── ATS.Recruitment.Infrastructure/ # EF repository, LocalFileStorage, SmtpEmailSender
│   │
│   ├── AiScreening/                     # ⭐ Lõi AI — giữ Clean Architecture 100%
│   │   ├── ATS.AiScreening.Domain/      #   AiScore, ScreeningJob, InterviewQuestion, CacheKey,
│   │   │                                #   AnonymizedCv (ctor internal) + IAnonymizer +
│   │   │                                #   SimpleAnonymizer + IPiiRedactor  ← xem ADR-3,
│   │   │                                #   IAiScoringService, IInterviewQuestionGenerator,
│   │   │                                #   IScreeningQueue, IAiScoreCacheRepository, IAiUsageQuota
│   │   ├── ATS.AiScreening.Application/ #   CQRS: StartScreeningHandler, ProcessScreeningHandler,
│   │   │                                #   GenerateInterviewQuestionsHandler, PreviewScoreHandler
│   │   └── ATS.AiScreening.Infrastructure/ # OpenAiScoringAdapter, EmbeddingScoringAdapter,
│   │                                       # KeywordScoringAdapter, AiScoringPipeline,
│   │                                       # OpenAiInterviewQuestionAdapter, RedisScreeningQueue,
│   │                                       # LlmPiiRedactor (phase 2)
│   │
│   └── Identity/                        # Tài khoản 2 vai (HR, Ứng viên) + Admin
│       ├── ATS.Identity.Domain/
│       ├── ATS.Identity.Application/
│       └── ATS.Identity.Infrastructure/
│
├── Hosts/
│   ├── ATS.Api/                         # REST API + Composition Root (DI)
│   ├── ATS.Worker/                      # Background service tiêu thụ Redis queue
│   └── ATS.Web/                         # Blazor Server (giao diện cho cả HR và Ứng viên)
│
└── Tests/
    ├── ATS.AiScreening.Tests/           # Test lõi AI với FakeAdapter
    ├── ATS.Recruitment.Tests/           # Test nghiệp vụ CRUD
    ├── ATS.ArchitectureTests/           # Ép ranh giới bằng reflection (rules dưới)
    └── ATS.IntegrationTests/            # End-to-end: upload CV → chấm → xếp hạng
```

### 3.1. Năm quy tắc kiến trúc bắt buộc (ép bằng test trong CI)

Ba quy tắc cốt lõi:

1. **Domain không được reference Infrastructure.** Dependency luôn hướng vào trong.
2. **`IAiScoringService` và `IInterviewQuestionGenerator` chỉ nhận `AnonymizedCv`, không nhận
   `string` hay `Cv` thô.** Compiler ép PII protection.
3. **Controllers không được đụng thẳng `DbContext`.** Phải qua Application layer.

Hai quy tắc giữ cho ADR-3 không bị phá ngầm:

4. **Module `Recruitment` không được tham chiếu `AiScreening`.** Cross-module đi qua
   `IApplicationScreeningTrigger`. Vi phạm quy tắc này là `AnonymizedCv` rò sang module khác.
5. **`AnonymizedCv` không có constructor `public`, và `ATS.AiScreening.Domain` không được khai
   báo `InternalsVisibleTo` cho bất kỳ assembly nào.** Đây là quy tắc dễ bị phá ngầm nhất: chỉ
   cần một dòng `InternalsVisibleTo` là toàn bộ bảo đảm compile-time của RB3 biến mất mà không
   ai nhận ra, vì code vẫn biên dịch và mọi test khác vẫn xanh.

**Cài đặt: reflection thuần, không dùng thư viện ngoài.** Bản nháp đầu định dùng NetArchTest,
nhưng ba trong năm quy tắc trên (2, 4, 5) không diễn đạt được bằng thư viện đó — chúng cần đọc
chữ ký method và attribute ở mức assembly. Dùng `System.Reflection` trực tiếp vừa đủ sức diễn
đạt cả năm, vừa bớt một dependency.

`ATS.ArchitectureTests` gồm 9 test (quy tắc 1 chạy trên 4 assembly Domain, quy tắc 2 chạy trên
2 port AI). **Các test này đã được kiểm chứng bằng cách cố tình phá luật**: sửa
`IAiScoringService` nhận thêm `string` và thêm `InternalsVisibleTo` thì đúng 2 test đỏ, 7 test
còn lại vẫn xanh. Một bộ test kiến trúc chưa bao giờ đỏ là một bộ test chưa được chứng minh.

### 3.2. Về DbContext

**Một `AtsDbContext` dùng chung, tách bảng bằng schema PostgreSQL**, đặt trong project riêng
`src/Shared/ATS.Persistence`:

- Schema `identity.*`: `users`, `password_reset_tokens`
- Schema `recruitment.*`: `candidates`, `hr_profiles`, `cvs`, `jobs`, `applications`,
  `interviews`, `evaluations`
- Schema `aiscreening.*`: `ai_scores`, `ai_score_cache`, `screening_jobs`,
  `interview_questions`, `ai_usage_quotas`

`identity` chỉ giữ thứ phục vụ xác thực (`users` = email + mật khẩu + vai + token đặt lại mật
khẩu). Hồ sơ nghiệp vụ (`candidates`, `hr_profiles`) thuộc `recruitment`, vì `Candidate` là
entity của `Recruitment.Domain` (`ICandidateRepository` nằm ở đó) — để ở `identity` thì
Recruitment phải ghi vào schema của module khác.

`ai_score_cache` **tách hẳn** khỏi `ai_scores`: cache khoá theo *nội dung* (CV + JD + model +
prompt), còn `ai_scores` khoá theo *đơn ứng tuyển*. Ứng viên preview khi chưa nộp đơn nên
không có `application_id` nào để ghi — chi tiết ở
[`database-design.md`](database-design.md) mục 2.

Lý do (đã cân nhắc phương án per-module DbContext):

- Migration đơn giản hơn (1 chuỗi migration thay vì 3)
- Join cross-module (ví dụ báo cáo) không cần federated query
- Vẫn thấy ranh giới trong ERD (schema là "hàng rào mềm")
- Team sinh viên chưa quen — giảm gánh nặng

#### Vì sao `AtsDbContext` cần một project riêng

Một DbContext dùng chung cần một chỗ ở chung. Ba phương án đã cân nhắc:

| Phương án | Vì sao loại |
|---|---|
| Đặt trong `Recruitment.Infrastructure` | Hai module còn lại phải reference chéo sang nó — vi phạm mục 3.3 |
| Đặt trong `ATS.SharedKernel` | Kéo EF Core vào SharedKernel, mà mọi Domain đều reference SharedKernel → Domain gián tiếp phụ thuộc EF, vi phạm quy tắc 1 |
| Project riêng `ATS.Persistence` | **Đã chọn.** Chỉ ba project Infrastructure reference nó; Domain không thấy nó |

Cấu hình entity **không** nằm trong `ATS.Persistence`. Mỗi module tự viết
`IEntityTypeConfiguration` trong Infrastructure của mình rồi đăng ký assembly với
`AtsDbContextConfigurator` ở Composition Root. Nhờ vậy `AtsDbContext` không phải tham chiếu
project của module nào — đúng chiều phụ thuộc.

Đánh đổi: nếu sau này tách microservices, phải chia lại DbContext. Chấp nhận vì đây là đồ án
học, không phải sản phẩm production.

### 3.3. Ranh giới module

Ba module **không được reference chéo project của nhau**. Nếu module A cần dữ liệu của module B,
đi qua **interface public** đặt trong `A.Application` và implement ở B (adapter mỏng).

Ví dụ: `Recruitment.Application.IApplicationScreeningTrigger` được implement bởi
`AiScreening.Infrastructure.ScreeningTriggerAdapter` — vì `Recruitment` không được biết gì về
Redis, LLM.

---

### 3.4. Vì sao PostgreSQL, không phải SQL Server

Mặc định của hệ sinh thái .NET là SQL Server, nên chọn khác thì phải giải thích được.

| Phương án | Vì sao loại |
|---|---|
| SQL Server (Developer Edition) | Image ~1,5 GB, khuyến nghị tối thiểu 2 GB RAM cho riêng nó — vi phạm RB4 (máy 8 GB còn phải chạy api + worker + web + Redis + IDE) |
| SQLite | Không có schema để tách module, không partial index, kém khi worker và api cùng ghi |
| MySQL 8 | Được, nhưng không có partial unique index (cần cho `cvs.is_default`) và JSON yếu hơn JSONB |

Ba thứ cụ thể của PostgreSQL mà thiết kế này đang dựa vào:

1. **Schema** làm hàng rào mềm giữa 3 module trong cùng một database (mục 3.2).
2. **Partial unique index** cho ràng buộc "mỗi ứng viên chỉ 1 CV mặc định":
   `CREATE UNIQUE INDEX ... ON cvs (candidate_id) WHERE is_default` — SQL Server có filtered
   index tương đương, MySQL thì không có.
3. **`postgres:16-alpine` ~ 80 MB**, `pg_isready` dùng làm healthcheck trong compose, license
   miễn phí không ràng buộc — hợp RB4 và RB5.

**Dấu hiệu phải xem lại:** khi trường hoặc doanh nghiệp yêu cầu bắt buộc SQL Server. EF Core
làm việc đổi provider không quá đắt, nhưng phải viết lại partial index và 3 câu `CREATE SCHEMA`.

### 3.5. Phiên bản .NET

Toàn bộ solution dùng **.NET 10 (LTS)**. `TargetFramework` khai báo **một chỗ duy nhất** trong
`Directory.Build.props` ở thư mục gốc, không lặp lại trong 18 file `.csproj`.

Lý do không chọn .NET 8: học phần kéo dài 10 tuần và kết thúc cuối tháng 11/2026 — **đúng lúc
.NET 8 hết hạn hỗ trợ**. Bảo vệ một đồ án trên nền tảng vừa hết hỗ trợ là điểm trừ không cần
thiết. .NET 10 được hỗ trợ tới tháng 11/2028.

Đổi phiên bản về sau chỉ cần sửa 4 chỗ: `Directory.Build.props`, `docker/Dockerfile` (2 dòng
`FROM`), `.github/workflows/ci.yml`, `.github/workflows/ai-smoke.yml`.

---

## 4. Chuỗi trạng thái

Hệ thống có **hai chuỗi trạng thái độc lập** — quan trọng để không dính lỗi thiết kế RB9.

### 4.1. Trạng thái ứng tuyển (nghiệp vụ, HR điều khiển)

```
Đã nộp → Sàng lọc → Phỏng vấn → Nhận
              ↘             ↘
                Từ chối       Từ chối
```

### 4.2. Trạng thái sàng lọc AI (kỹ thuật, hệ thống điều khiển)

```
Chưa chấm → Đang chấm → Đã chấm
                    ↘
                      Không chấm được
```

**Hai chuỗi này không được ràng buộc lẫn nhau.** HR vẫn mở CV, vẫn chuyển trạng thái ứng tuyển
được khi điểm AI còn `Chưa chấm` hoặc `Không chấm được`. Đây là RB9.

---

## 5. Deployment

`docker/docker-compose.yml` gom 6 container:

```
                              ┌─── OpenAI ───┐   ← preview 1 CV, đồng bộ
                              │              │     (ngoại lệ ADR-2, UC-05)
┌──────────┐  ┌──────────┐  ┌─┴────────┐  ┌──▼───────┐
│  Web     │─▶│  Api     │──┤ (LLM)    │  │  Worker  │  ← sàng lọc lô
│ (Blazor) │  │ (ASP.NET)│  └──────────┘  │ (BgSvc)  │
└──────────┘  └────┬─────┘                └────┬─────┘
                   │                           │
      ┌────────────┼───────────────┬───────────┤
      ▼            ▼               ▼           ▼
 ┌────────┐   ┌────────┐     ┌──────────┐ ┌────────┐
 │   db   │   │ queue  │     │ mailhog  │ │   db   │
 │ (pg16) │   │(redis) │     │  (smtp)  │ │        │
 └────────┘   └────────┘     └──────────┘ └────────┘
```

| Cổng | Dịch vụ |
|---|---|
| 8080 | API + Swagger |
| 8081 | Web (Blazor) |
| 8025 | MailHog — đọc email khi dev |
| 5432 / 6379 | PostgreSQL / Redis |

`mailhog` chỉ phục vụ môi trường dev: mọi email mời phỏng vấn và đặt lại mật khẩu rơi vào đó
thay vì gửi ra Internet. Khi deploy thật thì trỏ `Smtp__Host` sang SMTP thật và bỏ container
này đi.

Web và Api có thể gộp vào một process trong bản demo (nếu thiếu RAM), nhưng thiết kế vẫn tách để
sau này scale được. Worker bắt buộc riêng để không ngốn CPU/RAM của API khi chạy sàng lọc lô.

---

## 6. Đánh đổi và nợ kỹ thuật đã biết

| Chấp nhận | Cái giá | Khi nào xem lại |
|---|---|---|
| Modular Monolith, không microservices | Một bug có thể kéo cả API xuống | Khi có > 5 module |
| 1 DbContext + schema | Khó tách microservices sau này | Khi module cần DB riêng |
| Application layer mỏng ở Recruitment | Không "chuẩn Clean" 100% | Khi nghiệp vụ phức tạp lên |
| Blazor Server (không SSR/SPA) | Không tối ưu SEO cho trang tin công khai | Khi cần index Google |
| Polling `/screening-jobs/{id}` | Latency 2–5s | Khi > 100 concurrent user chờ kết quả |
| Redis là single-node | Mất queue nếu Redis crash | Khi cần HA |
| Preview gọi LLM đồng bộ trong API (ngoại lệ ADR-2) | API phải giữ khoá LLM; một request chiếm luồng tới 30s | Khi p95 preview > 30s, hoặc > 50 ứng viên preview đồng thời |
| Xếp hạng ứng viên bằng JOIN `applications` ↔ `ai_scores` | Thêm một phép join mỗi lần HR mở danh sách | Khi một tin có > 5.000 hồ sơ |
| `interview_questions` ở schema `aiscreening` nhưng FK trỏ sang `recruitment.interviews` | Khoá ngoại xuyên schema — vướng khi tách service | Khi tách module thành service riêng |

---

## 7. Tham chiếu

- Contracts và DTO chi tiết: [`contracts.md`](contracts.md)
- ERD: [`database-design.md`](database-design.md)
- Prompt và fallback AI: [`ai-integration.md`](ai-integration.md)
- Kế hoạch triển khai: [`weekly-plan.md`](weekly-plan.md)
