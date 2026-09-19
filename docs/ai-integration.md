# Tích hợp AI

> TV4 phụ trách. Cập nhật tuần 5–8.

Hệ thống có **2 năng lực AI**, cả hai đứng sau Port (xem `architecture.md` ADR-3):

1. **Chấm độ phù hợp CV ↔ JD** — dùng bởi cả HR (sàng lọc lô) và Ứng viên (preview trước khi nộp)
2. **Sinh câu hỏi phỏng vấn** — dùng bởi HR khi hẹn phỏng vấn

## 1. Pipeline chấm phù hợp

Cùng một pipeline, **hai đường vào khác nhau** (xem `architecture.md` ADR-2):

| Đường vào | Ai gọi | Chạy ở đâu | Phản hồi |
|---|---|---|---|
| Sàng lọc lô | HR, 200–300 hồ sơ | `ATS.Worker` qua Redis queue | `202` + polling |
| Preview (UC-05) | Ứng viên, 1 cặp CV↔JD | **`ATS.Api`, đồng bộ**, timeout 30s | `200 OK` |

Cả hai đều đi qua đúng các bước dưới đây — khác nhau chỉ ở process chạy nó và ở chỗ bước [5]
có ghi `ai_scores` hay không.

```
Input: rawCv (text từ PDF/DOCX) + JobRequirement (JD)
   │
   ▼
[1] Trích xuất text từ CV file (PdfPig / OpenXml)
   │
   ▼
[2] IAnonymizer.AnonymizeAsync — bỏ tên, SĐT, email, địa chỉ, tên trường/công ty
   │  → AnonymizedCv (value object, ctor internal, compile-time guarantee)
   │  ⚠ Port VÀ bản hiện thực regex đều nằm trong AiScreening.Domain — xem mục 3
   │
   ▼
[3] Tính CacheKey = SHA256(cvText, jdText, modelVersion, promptVersion)
   │  → IAiScoreCacheRepository.GetAsync(key)
   │  → Trúng cache: tăng hit_count, trả ngay, KHÔNG gọi LLM
   │
   ▼
[4] AiScoringPipeline.ScoreAsync(anonymizedCv, jd):
   │
   │   Adapter 1: OpenAiScoringAdapter
   │     └─ gpt-4o-mini, structured output
   │     └─ retry 3 lần với exponential backoff
   │     └─ fail → Adapter 2
   │
   │   Adapter 2: EmbeddingScoringAdapter
   │     └─ text-embedding-3-small, cosine similarity
   │     └─ score = round(similarity * 100)
   │     └─ summary = template
   │     └─ fail → Adapter 3
   │
   │   Adapter 3: KeywordScoringAdapter (LUÔN chạy được)
   │     └─ đếm keyword từ JD.RequiredSkills có trong CV
   │     └─ score = matchedCount / totalRequired * 100
   │
   ▼
[5] Ghi kết quả — hai chỗ khác nhau, đừng nhầm:
   │
   │   a) ai_score_cache  ← LUÔN ghi (trừ khi Keyword adapter: miễn phí, không cần cache)
   │      khoá theo cache_key, KHÔNG có application_id
   │
   │   b) ai_scores       ← CHỈ ghi khi đi từ đường sàng lọc lô (đã có Application)
   │      khoá theo application_id, kèm cache_id trỏ về (a)
   │
   │   Preview của ứng viên dừng ở (a). Đây chính là lý do cache phải là
   │   bảng riêng: lúc preview chưa tồn tại Application nào để tham chiếu.
   │
   ▼
Output: ScreeningResult(Score, Summary, Strengths, Gaps, AdapterUsed)
```

## 2. Prompt (v1)

### 2.1. Chấm phù hợp — `prompts/scoring-v1.txt`

```
Bạn là chuyên gia tuyển dụng. Chấm điểm mức độ phù hợp giữa CV và mô tả công việc,
theo thang 0-100. Trả về JSON strict:

{
  "score": <0-100>,
  "summary": "<1-2 câu tổng kết>",
  "strengths": ["<điểm mạnh 1>", "<điểm mạnh 2>", ...],
  "gaps": ["<kỹ năng thiếu 1>", ...]
}

Quy tắc chấm:
- 90-100: khớp hoàn toàn, có thể vào phỏng vấn ngay
- 70-89: khớp phần lớn, thiếu 1-2 kỹ năng phụ
- 50-69: khớp một nửa, cần training
- <50: không phù hợp

Không được nêu tên riêng của ứng viên hoặc công ty trong summary.
Không được đề xuất mức lương.

--- CV (đã ẩn danh) ---
{{cv_text}}

--- JD ---
Vị trí: {{job_title}}
Yêu cầu: {{job_requirements}}
Kỹ năng bắt buộc: {{required_skills}}
Kỹ năng ưu tiên: {{nice_to_have_skills}}
Kinh nghiệm tối thiểu: {{min_years}} năm
```

### 2.2. Sinh câu hỏi phỏng vấn — `prompts/questions-v1.txt`

```
Bạn là chuyên gia phỏng vấn. Dựa trên CV (đã ẩn danh) và JD, sinh 8-12 câu hỏi phỏng vấn.
Chia làm 3 nhóm:
- technical (5-7 câu): kiểm tra kỹ năng chuyên môn cụ thể có trong CV & JD
- behavioral (2-3 câu): kiểm tra soft skill, kinh nghiệm làm việc nhóm
- situational (1-2 câu): tình huống cụ thể khi làm ở vị trí này

Trả về JSON strict:
{
  "questions": [
    {"category": "technical", "question": "..."},
    ...
  ]
}

Quy tắc:
- Không câu hỏi yes/no
- Câu hỏi phải bám sát kỹ năng ứng viên đã ghi trong CV (đừng hỏi thứ họ không đề cập)
- Không hỏi thông tin cá nhân, tuổi tác, tôn giáo, hôn nhân
- Câu hỏi bằng tiếng Việt

--- CV (đã ẩn danh) ---
{{cv_text}}

--- JD ---
Vị trí: {{job_title}}
Yêu cầu: {{job_requirements}}
```

## 3. Ẩn danh (Anonymizer)

`IAnonymizer` **và** `SimpleAnonymizer` đều nằm trong **`AiScreening.Domain`**, không phải
Infrastructure. Lý do đầy đủ ở `architecture.md` ADR-3; tóm tắt: constructor của
`AnonymizedCv` là `internal`, nên chỉ code cùng assembly mới tạo được. Đặt `SimpleAnonymizer`
ở Infrastructure thì phải mở `InternalsVisibleTo` — và bảo đảm compile-time mà cả đồ án dựa
vào sẽ chỉ còn là hình thức. `SimpleAnonymizer` là regex thuần, không I/O, không SDK ngoài,
nên đặt trong Domain hoàn toàn hợp lệ.

`SimpleAnonymizer` phase 1 dùng regex:

- Email → `[EMAIL]`
- SĐT Việt Nam (`0\d{9,10}`, `+84\d{9,10}`) → `[PHONE]`
- Tên (regex đơn giản: 2-4 từ ghép, viết hoa chữ cái đầu, ở đầu CV) → `[NAME]`
- Địa chỉ (từ khóa "Địa chỉ:", "Address:") → `[ADDRESS]`
- Tên trường (từ khóa "Đại học", "University") → `[SCHOOL]` (giữ lại "chuyên ngành X" nếu có)
- Tên công ty cũ (khó tự động) → **HR duyệt lại tay khi thấy sai**

### Phase 2 — ẩn danh bằng LLM mà không phá bảo đảm compile-time

Cách sai: viết `LlmAnonymizer : IAnonymizer` ở Infrastructure. Làm vậy buộc phải mở
`InternalsVisibleTo` cho assembly đó, và từ lúc ấy bất kỳ class nào trong Infrastructure cũng
tự dựng được `AnonymizedCv` từ CV thô.

Cách đúng: thêm một port **cấp thấp hơn, chỉ làm việc trên `string`**:

```csharp
// AiScreening.Domain
public interface IPiiRedactor
{
    Task<string> RedactAsync(string rawCvText, CancellationToken ct);
}
```

`LlmPiiRedactor` (Infrastructure) implement port này. `SimpleAnonymizer` trong Domain gọi nó
trước, rồi chạy tiếp regex như lưới an toàn, rồi mới bọc thành `AnonymizedCv`:

```
rawCv ──► LlmPiiRedactor ──► regex (lưới thứ hai) ──► AnonymizedCv
          (Infrastructure)    (Domain)                 (chỉ Domain tạo được)
```

Infrastructure xử lý được text nhưng không bao giờ cầm quyền tạo `AnonymizedCv`. Nếu chưa
cấu hình `IPiiRedactor` thì chỉ regex chạy — pipeline vẫn hoạt động.

## 4. Cache & chi phí

### CacheKey

```csharp
CacheKey = SHA256(
    normalizedCvText,   // lowercased, whitespace-collapsed
    normalizedJdText,
    modelVersion,       // "gpt-4o-mini-2024-07-18"
    promptVersion       // "scoring-v1"
)
```

Cache lưu ở bảng riêng **`aiscreening.ai_score_cache`**, không dùng Redis (cần persist qua
restart và join được khi làm báo cáo chi phí).

```sql
SELECT score, summary, strengths, gaps, adapter_used
FROM   aiscreening.ai_score_cache
WHERE  cache_key = @key;
```

**Vì sao không gộp vào `ai_scores`:** `ai_scores.application_id` là khoá và là FK bắt buộc,
trong khi UC-05 cho ứng viên xem điểm **trước khi nộp đơn** — lúc đó chưa có `Application`
nào tồn tại, không có gì để ghi vào `application_id`. Gộp chung thì toàn bộ chiến lược tiết
kiệm chi phí của RB2 sập ở đúng use case tốn tiền nhất (ứng viên bấm thử nhiều lần).

Hai bảng, hai vai trò: `ai_score_cache` khoá theo **nội dung**, `ai_scores` khoá theo **đơn
ứng tuyển**. Chi tiết ở `database-design.md` mục 2.

### Ước lượng chi phí (RB2)

| Loại | Model | Token in | Token out | Giá / 1M | / lần |
|---|---|---|---|---|---|
| Chấm phù hợp | gpt-4o-mini | ~1200 | ~250 | in $0.15, out $0.60 | ~$0.0004 |
| Embedding | text-emb-3-small | ~1500 | 0 | $0.02 | ~$0.00003 |
| Sinh câu hỏi | gpt-4o-mini | ~1500 | ~800 | in $0.15, out $0.60 | ~$0.0007 |

**Ước cả kỳ:**
- 1000 lượt chấm (có cache tốt) ≈ $0.40
- 200 lượt sinh câu hỏi ≈ $0.14
- Test và dev ≈ $5 (dùng FakeAdapter là chính)
- **Tổng ~ $10–20** — trong ngân sách $20–30.

### Rate limit

Bộ đếm lưu ở bảng **`aiscreening.ai_usage_quotas`**, khoá `(user_id, usage_date)` — không để
trong bộ nhớ, vì api và worker là hai process riêng và restart thì mất sạch.

| Đối tượng | Hạn mức | Cột đếm |
|---|---|---|
| Ứng viên — preview | 20 lượt/ngày | `preview_count` |
| HR — chạy lô | 5 lô/ngày, tối đa 500 hồ sơ mỗi lô | `batch_count` |
| Toàn hệ thống | cảnh báo khi chi phí tích luỹ vượt $25 | truy vấn tổng trên `ai_score_cache` |

Tăng bộ đếm bằng một câu lệnh nguyên tử, không cần lock ở tầng ứng dụng:

```sql
INSERT INTO aiscreening.ai_usage_quotas (id, user_id, usage_date, preview_count)
VALUES (gen_random_uuid(), @userId, CURRENT_DATE, 1)
ON CONFLICT (user_id, usage_date)
DO UPDATE SET preview_count = ai_usage_quotas.preview_count + 1,
              updated_at    = now()
RETURNING preview_count;
```

Nếu giá trị trả về vượt trần → API trả `429 Too Many Requests`. **Trúng cache thì không tính
vào hạn mức** — ứng viên xem lại điểm cũ bao nhiêu lần cũng được, vì không tốn tiền.

Port: `IAiUsageQuota` (xem `contracts.md` mục 2.2).

## 5. Fallback 3 tầng — bài học quan trọng

Nếu chỉ có OpenAI adapter và hết quota, cả hệ thống chết. Fallback 3 tầng đảm bảo **cuối cùng
luôn có điểm** (dù chất lượng giảm):

| Tầng | Adapter | Có LLM? | Chi phí | Chất lượng |
|---|---|---|---|---|
| 1 | OpenAiScoringAdapter | Có | ~$0.0004/lần | Cao |
| 2 | EmbeddingScoringAdapter | Có (embedding rẻ) | ~$0.00003/lần | Trung bình |
| 3 | KeywordScoringAdapter | **Không** | $0 | Thấp nhưng có |

Adapter cuối không phụ thuộc mạng, không tốn tiền, không bao giờ fail. Đây là yếu tố sống còn
của hệ thống — HR không bao giờ thấy màn hình "AI đang lỗi, thử lại sau".

### Bắt buộc: luôn hiển thị điểm đến từ tầng nào

Ba tầng cho ra **ba thang điểm khác nhau**. Điểm 72 của `KeywordScoringAdapter` (tỉ lệ khớp từ
khoá) và điểm 72 của `OpenAiScoringAdapter` (đánh giá ngữ nghĩa) không cùng một đơn vị. Nếu UI
chỉ hiện con số trần thì HR sẽ xếp hạng bằng cách so hai đại lượng không so được với nhau — và
sẽ loại nhầm ứng viên.

Vì vậy `adapter_used` **không phải cột debug**, nó là một phần của kết quả:

- `ScorePreviewResponse` và `ApplicationDto` đều mang trường `AdapterUsed` (`contracts.md` mục 4)
- UI hiện nhãn cạnh điểm: **AI** (OpenAI) · **Ngữ nghĩa** (Embedding) · **Từ khoá** (Keyword)
- Nhãn `Từ khoá` kèm chú thích "độ chính xác thấp — nên đọc CV trực tiếp"
- Khi một danh sách trộn nhiều tầng, bảng xếp hạng hiện cảnh báo ở đầu

Đây cũng là câu hỏi dễ bị hỏi nhất khi bảo vệ: *"ba tầng fallback cho ba thang điểm khác nhau,
HR phân biệt bằng cách nào?"*

## 6. Testing

- **Unit test**: mock `IAiScoringService` → `FakeAiScoringAdapter` trả điểm cố định (dùng cho
  test business logic ở Application layer). Test không tốn tiền, chạy < 1s.
- **Smoke test gọi OpenAI thật**: **không** chạy theo mỗi lần merge vào `main`. Ba lý do:
  mỗi lần merge tốn tiền thật (RB2), test đỏ oan khi OpenAI chậm hoặc rate-limit, và khoá API
  phải nằm sẵn trong secrets của một repo sinh viên. Thay vào đó đặt ở workflow riêng
  `.github/workflows/ai-smoke.yml`, kích hoạt **thủ công** (`workflow_dispatch`) hoặc theo
  lịch mỗi tuần. Chạy trước buổi bảo vệ và sau mỗi lần đổi prompt version.
- **Snapshot test**: lưu 5 cặp (CV, JD) mẫu, so sánh output với snapshot đã duyệt. Rerun khi
  đổi prompt version.

## 7. Log & observability

Log mỗi lần gọi LLM (KHÔNG log CV text):
```
{
  "event": "ai.scoring",
  "cache_hit": false,
  "adapter": "OpenAI",
  "duration_ms": 1240,
  "score": 78,
  "cache_key": "abc123...",
  "model": "gpt-4o-mini",
  "prompt_version": "scoring-v1",
  "cost_estimate_usd": 0.0004
}
```

Dashboard đơn giản (Grafana hoặc chỉ query SQL): tổng chi phí ngày, tỉ lệ cache hit, phân phối
adapter được dùng, latency p95.

## 8. Bảo mật PII (RB3)

- CV thô (`raw_cv_text`) **không được** đưa vào prompt. Chỉ `AnonymizedCv.Text`.
- Kiểu `AnonymizedCv` có constructor `internal` → **chỉ code trong cùng assembly
  `ATS.AiScreening.Domain` tạo được**. Vì `SimpleAnonymizer` cũng nằm ở đó, không cần
  `InternalsVisibleTo` cho bất kỳ assembly nào. Application layer, Infrastructure và module
  `Recruitment` đều không thể "cast" từ `string` sang `AnonymizedCv` — không phải vì quy ước,
  mà vì không biên dịch được.
- ArchitectureTests ép: mọi method trong `Ports/*ScoringService*` và `Ports/*QuestionGenerator*`
  chỉ nhận `AnonymizedCv`, không nhận `string` hay `Cv`.
- Prompt hệ thống có câu "Không được nêu tên riêng" — nếu LLM lỡ lộ tên qua Strengths/Gaps thì
  vẫn có tuyến ẩn danh ở tầng dưới, coi như defense in depth.
