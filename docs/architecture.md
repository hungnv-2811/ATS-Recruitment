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

**Dấu hiệu phải xem lại:** khi latency polling gây khó chịu → chuyển sang SSE ở phase sau.

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
│   └── ATS.SharedKernel/                # KHÔNG chứa nghiệp vụ, chỉ primitive
│                                          # Entity, ValueObject, Result<T>, IUnitOfWork
├── Modules/
│   ├── Recruitment/                     # Nghiệp vụ tuyển dụng cốt lõi
│   │   ├── ATS.Recruitment.Domain/      #   Job, Candidate, Cv (1-N), Application, Interview,
│   │   │                                #   AnonymizedCv value object, IJobRepository, ...
│   │   ├── ATS.Recruitment.Application/ #   Service class + validator (không CQRS đầy đủ)
│   │   └── ATS.Recruitment.Infrastructure/ # EF repository, LocalFileStorage, SimpleAnonymizer
│   │
│   ├── AiScreening/                     # ⭐ Lõi AI — giữ Clean Architecture 100%
│   │   ├── ATS.AiScreening.Domain/      #   AiScore, ScreeningJob, InterviewQuestion,
│   │   │                                #   CacheKey, IAiScoringService, IInterviewQuestionGenerator,
│   │   │                                #   IScreeningQueue
│   │   ├── ATS.AiScreening.Application/ #   CQRS: StartScreeningHandler, ProcessScreeningHandler,
│   │   │                                #   GenerateInterviewQuestionsHandler
│   │   └── ATS.AiScreening.Infrastructure/ # OpenAiScoringAdapter, EmbeddingScoringAdapter,
│   │                                       # KeywordScoringAdapter, AiScoringPipeline,
│   │                                       # OpenAiInterviewQuestionAdapter, RedisScreeningQueue
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
    ├── ATS.ArchitectureTests/           # NetArchTest ép ranh giới (rules dưới)
    └── ATS.IntegrationTests/            # End-to-end: upload CV → chấm → xếp hạng
```

### 3.1. Ba quy tắc kiến trúc bắt buộc (ép bằng NetArchTest trong CI)

1. **Domain không được reference Infrastructure.** Dependency luôn hướng vào trong.
2. **`IAiScoringService` và `IInterviewQuestionGenerator` chỉ nhận `AnonymizedCv`, không nhận
   `string` hay `Cv` thô.** Compiler ép PII protection.
3. **Controllers không được đụng thẳng `DbContext`.** Phải qua Application layer.

### 3.2. Về DbContext

**Một `AtsDbContext` dùng chung, tách bảng bằng schema PostgreSQL:**

- Schema `recruitment.*`: `jobs`, `candidates`, `cvs`, `applications`, `interviews`, `evaluations`
- Schema `aiscreening.*`: `ai_scores`, `screening_jobs`, `interview_questions`
- Schema `identity.*`: `users`, `roles`

Lý do (đã cân nhắc phương án per-module DbContext):

- Migration đơn giản hơn (1 chuỗi migration thay vì 3)
- Join cross-module (ví dụ báo cáo) không cần federated query
- Vẫn thấy ranh giới trong ERD (schema là "hàng rào mềm")
- Team sinh viên chưa quen — giảm gánh nặng

Đánh đổi: nếu sau này tách microservices, phải chia lại DbContext. Chấp nhận vì đây là đồ án
học, không phải sản phẩm production.

### 3.3. Ranh giới module

Ba module **không được reference chéo project của nhau**. Nếu module A cần dữ liệu của module B,
đi qua **interface public** đặt trong `A.Application` và implement ở B (adapter mỏng).

Ví dụ: `Recruitment.Application.IApplicationScreeningTrigger` được implement bởi
`AiScreening.Infrastructure.ScreeningTriggerAdapter` — vì `Recruitment` không được biết gì về
Redis, LLM.

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

`docker/docker-compose.yml` gom 5 container:

```
┌──────────┐  ┌──────────┐  ┌──────────┐
│  Web     │  │  Api     │  │  Worker  │
│ (Blazor) │  │ (ASP.NET)│  │ (BgSvc)  │
└────┬─────┘  └────┬─────┘  └────┬─────┘
     │             │             │
     │             ├─────────────┤
     │             ▼             ▼
     │        ┌────────┐    ┌────────┐
     └───────▶│   db   │    │ queue  │
              │ (pg16) │    │(redis) │
              └────────┘    └────────┘
```

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

---

## 7. Tham chiếu

- Contracts và DTO chi tiết: [`contracts.md`](contracts.md)
- ERD: [`database-design.md`](database-design.md)
- Prompt và fallback AI: [`ai-integration.md`](ai-integration.md)
- Kế hoạch triển khai: [`weekly-plan.md`](weekly-plan.md)
