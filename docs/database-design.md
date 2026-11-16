# Thiết kế cơ sở dữ liệu

> TV1 phụ trách tài liệu này. Cập nhật tuần 3 (chốt quy ước tên cột/enum để SQL báo cáo và EF
> của các module khớp nhau — mục 2.1).

PostgreSQL 16. **Một `AtsDbContext` dùng chung, tách bảng theo schema** để giữ ranh giới module
mà vẫn dễ migration/join.

## 1. Sơ đồ ER (mermaid)

```mermaid
erDiagram
    USERS ||--o| CANDIDATES : "1 tài khoản ứng viên"
    USERS ||--o| HR_PROFILES : "1 tài khoản HR"
    CANDIDATES ||--o{ CVS : "có nhiều CV"
    CANDIDATES ||--o{ APPLICATIONS : "ứng tuyển nhiều tin"
    JOBS ||--o{ APPLICATIONS : "có nhiều đơn"
    CVS ||--o{ APPLICATIONS : "1 đơn dùng 1 CV"
    APPLICATIONS ||--o| AI_SCORES : "có tối đa 1 điểm AI"
    APPLICATIONS ||--o{ INTERVIEWS : "có nhiều buổi phỏng vấn"
    INTERVIEWS ||--o{ INTERVIEW_QUESTIONS : "có nhiều câu hỏi"
    INTERVIEWS ||--o| EVALUATIONS : "có 1 đánh giá"
    JOBS ||--o{ SCREENING_JOBS : "chạy sàng lọc theo tin"
    AI_SCORE_CACHE ||--o{ AI_SCORES : "nguồn kết quả (cache theo nội dung)"
    USERS ||--o{ PASSWORD_RESET_TOKENS : "token đặt lại mật khẩu"
    USERS ||--o{ AI_USAGE_QUOTAS : "hạn mức AI theo ngày"

    USERS {
        uuid id PK
        string email UK
        string password_hash
        string full_name "họ tên hiển thị — claim JWT 'name'"
        string role "Candidate|HR|Admin"
        bool is_active
        timestamp created_at
    }

    CANDIDATES {
        uuid id PK
        uuid user_id FK
        string full_name
        string phone
        string address
        timestamp created_at
    }

    HR_PROFILES {
        uuid id PK
        uuid user_id FK
        string full_name
        string department
    }

    CVS {
        uuid id PK
        uuid candidate_id FK
        string name "VD: 'CV Backend Java 2026'"
        string file_path
        string file_type "pdf|docx"
        int file_size
        string extracted_text
        bool is_default
        timestamp uploaded_at
    }

    JOBS {
        uuid id PK
        uuid hr_owner_id FK
        string title
        text description
        text requirements
        string location
        string employment_type
        string status "Draft|Open|Closed"
        timestamp posted_at
        timestamp closed_at
    }

    APPLICATIONS {
        uuid id PK
        uuid job_id FK
        uuid candidate_id FK
        uuid cv_id FK "CV nào được nộp cho tin này"
        string status "Applied|Screening|Interview|Hired|Rejected"
        text hr_note
        timestamp applied_at
        timestamp updated_at
    }

    AI_SCORES {
        uuid id PK
        uuid application_id FK
        int score "0-100"
        text summary
        text strengths
        text gaps
        string screening_status "Pending|Processing|Scored|Failed"
        uuid cache_id FK "kết quả lấy từ dòng cache nào"
        string model_version
        string adapter_used "OpenAI|Embedding|Keyword|Fake"
        bool is_human_reviewed
        timestamp scored_at
    }

    AI_SCORE_CACHE {
        uuid id PK
        string cache_key UK "SHA256(cv|jd|model|prompt)"
        int score "0-100"
        text summary
        text strengths
        text gaps
        string adapter_used "OpenAI|Embedding|Keyword"
        string model_version
        string prompt_version
        int hit_count "đo tỉ lệ cache hit cho RB2"
        timestamp created_at
        timestamp last_hit_at
    }

    AI_USAGE_QUOTAS {
        uuid id PK
        uuid user_id FK
        date usage_date
        int preview_count "UC-05, trần 20/ngày"
        int batch_count "HR chạy lô, trần 5/ngày"
        timestamp updated_at
    }

    PASSWORD_RESET_TOKENS {
        uuid id PK
        uuid user_id FK
        string token_hash UK "lưu hash, KHÔNG lưu token thô"
        timestamp expires_at "created_at + 30 phút"
        timestamp used_at "null nếu chưa dùng"
        timestamp created_at
    }

    SCREENING_JOBS {
        uuid id PK
        uuid job_id FK
        uuid started_by_hr_id FK
        int target_count
        int done_count
        int failed_count
        string status "Queued|Running|Done|Cancelled"
        timestamp started_at
        timestamp finished_at
    }

    INTERVIEWS {
        uuid id PK
        uuid application_id FK
        timestamp scheduled_at
        string mode "online|onsite"
        string location_or_link
        string interviewer_name
        string status "Scheduled|Done|Cancelled"
    }

    INTERVIEW_QUESTIONS {
        uuid id PK
        uuid interview_id FK
        string category "technical|behavioral|situational"
        text question
        string source "AI|Manual|Template"
        int order_index
    }

    EVALUATIONS {
        uuid id PK
        uuid interview_id FK
        int rating "1-5"
        text comment
        string recommendation "Hire|Reject|Consider"
        timestamp created_at
    }
```

## 2. Bảng theo schema

Nguyên tắc chia schema: **bảng thuộc schema của module sở hữu entity tương ứng trong code.**
`identity` chỉ giữ thứ phục vụ xác thực; hồ sơ nghiệp vụ thuộc `recruitment` vì `Candidate` là
entity của `Recruitment.Domain` (`ICandidateRepository` nằm ở đó, xem `contracts.md` mục 2.1).

### Schema `identity`

| Bảng | Mục đích |
|---|---|
| `identity.users` | Tài khoản chung — cả Ứng viên, HR, Admin: email + mật khẩu + vai + **`full_name`** |
| `identity.password_reset_tokens` | Token "quên mật khẩu", lưu **hash**, hết hạn 30 phút, dùng một lần |

> **Lệch so với ERD bản tuần 1 — có chủ đích:** `identity.users` có thêm cột `full_name`.
> JWT phải mang claim `name` (contracts.md mục 8.1) và Admin xem danh sách user kèm họ tên
> (`UserDto.FullName`) cho cả tài khoản HR/Admin — những tài khoản không có dòng `candidates`.
> Đọc họ tên từ `recruitment.*` thì Identity phải tham chiếu chéo module. `candidates.full_name`
> vẫn giữ (hồ sơ ứng viên có thể sửa riêng).

### Schema `recruitment`

| Bảng | Mục đích |
|---|---|
| `recruitment.candidates` | Hồ sơ cá nhân của Ứng viên (1-1 với `users` khi role=Candidate) |
| `recruitment.hr_profiles` | Hồ sơ HR (1-1 với `users` khi role=HR) |
| `recruitment.cvs` | Nhiều CV/ứng viên — điểm khác biệt so với bản trước |
| `recruitment.jobs` | Tin tuyển dụng, HR sở hữu |
| `recruitment.applications` | Bản đơn ứng tuyển: candidate + job + **cv** + trạng thái |
| `recruitment.interviews` | Buổi phỏng vấn của một application |
| `recruitment.evaluations` | Đánh giá sau phỏng vấn |

### Schema `aiscreening`

| Bảng | Mục đích |
|---|---|
| `aiscreening.ai_scores` | **Ảnh chụp** điểm AI của một đơn ứng tuyển — thứ HR đã nhìn thấy |
| `aiscreening.ai_score_cache` | **Kho tra cứu** theo nội dung, không gắn với đơn nào |
| `aiscreening.screening_jobs` | Lô sàng lọc HR chạy (tracking tiến độ) |
| `aiscreening.interview_questions` | Câu hỏi phỏng vấn (nguồn: AI/Manual/Template) |
| `aiscreening.ai_usage_quotas` | Bộ đếm hạn mức AI theo (user, ngày) — hiện thực RB2 |

### 2.1. Quy ước tên cột và kiểu (chốt tuần 3)

SQL báo cáo (`ATS.Persistence/Reporting`, TV1) viết tay theo đúng các quy ước này vì
`ATS.Persistence` không thấy entity của module. Đổi tên property entity = báo TV1 sửa SQL.

- Bảng/cột **snake_case tự động** (`UseSnakeCaseNamingConvention`): `HrOwnerId` → `hr_owner_id`.
  Mỗi `IEntityTypeConfiguration` chỉ đặt `ToTable("<tên>", AtsDbContext.<Schema>Schema)`.
- **Enum lưu dạng chuỗi** (`HasConversion<string>()`), giá trị đúng tên enum: `jobs.status` ∈
  `Draft|Open|Closed`, `applications.status` ∈ `Applied|Screening|Interview|Hired|Rejected`,
  `ai_scores.screening_status` ∈ `Pending|Processing|Scored|Failed`, `adapter_used` ∈
  `OpenAI|Embedding|Keyword|Fake`, `users.role` ∈ `Candidate|HR|Admin`.
- Mọi thời gian là `DateTimeOffset` UTC → `timestamp with time zone`. Khoá chính `uuid`, sinh ở
  code (`Guid.CreateVersion7()`).
- `jobs.hr_owner_id` = `identity.users.id` của HR (không phải `hr_profiles.id`); `ai_scores.score`
  cho phép NULL (chưa chấm / Failed).

| Truy vấn dashboard (`GET /api/reports/dashboard`) | Bảng / cột dùng |
|---|---|
| Số tin, tin đang mở | `recruitment.jobs(status, hr_owner_id)` |
| Đơn theo trạng thái, tỉ lệ Hired, top 5 tin | `recruitment.applications(job_id, status)` ⋈ `jobs` |
| Điểm AI theo trạng thái / adapter | `aiscreening.ai_scores(application_id, screening_status, adapter_used)` ⋈ `applications` ⋈ `jobs` |
| Tỉ lệ cache hit (RB2) = `Σhit_count / (Σhit_count + số dòng)` | `aiscreening.ai_score_cache(hit_count)` — luôn toàn hệ thống |

HR chỉ thấy số liệu của tin `hr_owner_id = <mình>`; Admin thấy toàn hệ thống.

#### `ai_scores` và `ai_score_cache` khác nhau chỗ nào

Đây là hai khái niệm khác nhau, gộp chung là sai:

| | `ai_score_cache` | `ai_scores` |
|---|---|---|
| Khoá theo | **Nội dung**: CV + JD + model + prompt | **Đơn ứng tuyển**: `application_id` |
| Tồn tại khi chưa có đơn? | **Có** — ứng viên preview trước khi nộp (UC-05) | Không |
| Dùng lại giữa nhiều đơn? | Có — 2 ứng viên nộp cùng một CV vào cùng tin thì trúng chung | Không |
| Khi đổi `prompt_version` | Dòng cũ thành vô dụng, ghi dòng mới | Giữ nguyên — đó là điểm HR đã thấy |

Vì vậy `ai_scores` sao chép lại `score`/`summary` tại thời điểm chấm thay vì chỉ trỏ sang
cache. Trùng lặp này **là cố ý**: nếu sau đó nhóm đổi prompt, hồ sơ vẫn còn đúng con số mà HR
đã căn cứ để ra quyết định.

## 3. Ràng buộc dữ liệu quan trọng

1. **`applications.cv_id` phải thuộc về đúng `applications.candidate_id`.** Enforce bằng trigger
   hoặc kiểm tra ở Application layer trước khi INSERT.
2. **`(application_id)` là UNIQUE trong `ai_scores`.** Một đơn có tối đa một điểm AI (chấm lại thì
   update, không insert dòng mới).
3. **`cache_key` là UNIQUE trong `ai_score_cache`**, giá trị
   `SHA256(cv_text || jd_text || model_version || prompt_version)`. Bảng này **không có**
   `application_id` — đó chính là lý do phải tách khỏi `ai_scores`: ứng viên xem preview khi
   chưa nộp đơn (UC-05), lúc đó chưa tồn tại `Application` nào để tham chiếu. Mọi lượt chấm,
   preview lẫn sàng lọc lô, đều tra bảng này trước khi gọi LLM.
4. **`cvs.is_default`**: chỉ 1 CV `is_default=true` per candidate (partial unique index).
5. **`applications` không được xóa vật lý**, chỉ set `status=Rejected` — để giữ lịch sử.
6. **`(user_id, usage_date)` là UNIQUE trong `ai_usage_quotas`.** Tăng bộ đếm bằng
   `INSERT ... ON CONFLICT (user_id, usage_date) DO UPDATE SET preview_count = preview_count + 1`
   — một câu lệnh nguyên tử, không cần lock ở tầng ứng dụng.
7. **`password_reset_tokens` lưu `token_hash`, không lưu token thô.** Rò database thì kẻ tấn
   công vẫn không đặt lại được mật khẩu. Token dùng một lần (`used_at`), hết hạn sau 30 phút.
8. **`ai_scores.cache_id` cho phép NULL.** Khi `KeywordScoringAdapter` chạy (không gọi LLM,
   không tốn tiền) thì không cần ghi cache.

## 4. Index

| Bảng | Cột | Loại | Vì sao |
|---|---|---|---|
| `applications` | `(job_id, status)` | btree | HR lọc theo tin + trạng thái |
| `applications` | `(candidate_id)` | btree | Ứng viên xem lịch sử |
| `ai_score_cache` | `cache_key` | unique btree | Lookup cache trước khi gọi LLM |
| `ai_scores` | `application_id` unique, `INCLUDE (score, screening_status)` | btree | Covering index cho truy vấn xếp hạng dưới đây |
| `ai_usage_quotas` | `(user_id, usage_date)` | unique btree | Đếm hạn mức trong ngày (RB2) |
| `password_reset_tokens` | `token_hash` | unique btree | Tra token khi đặt lại mật khẩu |
| `cvs` | `(candidate_id, is_default)` partial | btree | Tìm CV default |
| `users` | `email` | unique | Đăng nhập |

### 4.1. Vì sao không có index `(application_id, score DESC)`

Truy vấn nghiệp vụ thật không phải "xếp hạng theo `application_id`" mà là **"xếp hạng ứng viên
của tin X theo điểm giảm dần"**. Mà `ai_scores` **không có** cột `job_id`:

```sql
SELECT a.id, a.candidate_id, s.score, s.summary, s.adapter_used
FROM   recruitment.applications a
LEFT   JOIN aiscreening.ai_scores s ON s.application_id = a.id
WHERE  a.job_id = @jobId
ORDER  BY s.score DESC NULLS LAST;
```

Lọc xảy ra ở `applications(job_id, status)`; `ai_scores` chỉ bị tra theo `application_id`, mà
cột đó là UNIQUE — mỗi khoá đúng một dòng, nên thêm `score DESC` vào index **không sắp xếp
được gì cả**. Index đúng là index **covering**: `INCLUDE (score, screening_status)` để phép
join lấy luôn điểm mà không phải đọc heap.

`LEFT JOIN` + `NULLS LAST` là bắt buộc, không phải tuỳ chọn: hồ sơ chưa chấm hoặc "Không chấm
được" vẫn phải hiện trong danh sách (RB9).

**Dấu hiệu phải xem lại:** khi một tin có > 5.000 hồ sơ và phép join thành nút cổ chai → lúc
đó mới thêm `job_id` denormalized vào `ai_scores` kèm index `(job_id, score DESC)`, và chấp
nhận phải giữ đồng bộ cột đó.

## 5. Migration

**Hiện trạng (tuần 3):** chỉ có `20260919131605_InitialCreate` (tuần 2) — **chỉ tạo 3 schema**,
chưa có bảng nào. EF không tự sinh `EnsureSchema` khi model chưa có entity, nên ba dòng đó viết
tay trong `Up()`.

**Quy trình đã chốt:** tuần 3 ba nhánh (Identity/Recruitment của TV2, AiScreening của TV4, hạ
tầng của TV1) phát triển song song và **không nhánh nào tự tạo migration** — ba nhánh cùng sửa
`AtsDbContextModelSnapshot.cs` thì merge chắc chắn xung đột và snapshot sai. Sau khi merge đủ
các nhánh vào `nvhung`, TV1 sinh **một** migration duy nhất cho toàn bộ bảng
(`dotnet ef migrations add ... --project src/Shared/ATS.Persistence --startup-project src/Hosts/ATS.Api`),
rồi chạy thử trên Postgres thật (compose) trước khi đẩy lên.

- Migration nằm trong `ATS.Persistence/Migrations`, startup project là `ATS.Api` (Composition Root
  thật — model lúc sinh migration và lúc chạy là một).
- Áp lúc khởi động **chỉ khi** `Database:AutoMigrate=true` (compose bật sẵn cho dev/demo), và
  **chỉ API** migrate — worker không bao giờ migrate (tránh hai process tranh khoá
  `__EFMigrationsHistory`).
- Không split migration theo module ở phase 1 — split khi nào thực sự tách microservices.

Ba thứ EF Core không sinh được từ model, phải viết tay bằng `migrationBuilder.Sql(...)` trong
migration tích hợp đó:

```sql
CREATE UNIQUE INDEX ix_cvs_default ON recruitment.cvs (candidate_id) WHERE is_default;

ALTER TABLE recruitment.applications ADD CONSTRAINT ck_applications_cv_owner
  CHECK (public.fn_cv_belongs_to_candidate(cv_id, candidate_id));  -- ràng buộc 1, mục 3

CREATE INDEX ix_ai_scores_app ON aiscreening.ai_scores (application_id)
  INCLUDE (score, screening_status);
```

## 6. Seed data cho dev

Seed viết bằng C# (`ATS.Persistence/DatabaseSeeder.cs`), **không** phải `docker/seed.sql`: script
SQL chạy lúc container `db` khởi động sẽ chạy TRƯỚC khi API migrate, tức là trước khi bảng tồn
tại; và hash mật khẩu (BCrypt) phải sinh giống hệt cách Identity kiểm tra.

- Chạy trong API, **sau** `MigrateAsync`, khi `Database:AutoMigrate=true` **và**
  `Database:SeedDemoData=true` (compose: `DB_SEED_DEMO_DATA`, mặc định `true` cho dev).
- **Idempotent**: kiểm tra tồn tại trước khi thêm — khởi động lại container không nhân đôi dữ liệu.
- Tài khoản Admin lấy từ `Seed:AdminEmail` / `Seed:AdminPassword` (mặc định dev
  `admin@ats.local` / `Admin@123` — deploy thật **phải** đổi trong `docker/.env`).

Dữ liệu demo dự kiến (hoàn thiện ở pha tích hợp, sau khi có entity của mọi module):
- 1 admin, 3 HR test
- 5 ứng viên test, mỗi ứng viên 2 CV giả (đã có `extracted_text`)
- 10 tin tuyển dụng
- 30 application phân bổ đủ các trạng thái
- 30 dòng `ai_score_cache` tương ứng → demo chạy được **không cần** `OPENAI_API_KEY`

**Trạng thái tuần 3:** `DatabaseSeeder.SeedAsync` mới là khung rỗng (chưa có bảng để seed).

## 7. Backup & retention

- CV file gốc: lưu trong volume `cv-files` (`/app/data/cvs`, khoá `FileStorage:RootPath`) — không
  backup (đồ án học). `cvs.file_path` chỉ chứa **tên file do hệ thống sinh** (Guid v7 + đuôi), tương
  đối với thư mục gốc; `LocalFileStorage` từ chối mọi đường dẫn tuyệt đối hoặc thoát ra ngoài gốc.
- Database: `pg_dump` thủ công trước mỗi lần deploy demo
- PII: không log query có `full_name`, `phone`, `email` (RB3)
