# Tích hợp AI

> TV4 phụ trách. Cập nhật tuần 5–8.

Hệ thống có **2 năng lực AI**, cả hai đứng sau Port (xem `architecture.md` ADR-3):

1. **Chấm độ phù hợp CV ↔ JD** — dùng bởi cả HR (sàng lọc lô) và Ứng viên (preview trước khi nộp)
2. **Sinh câu hỏi phỏng vấn** — dùng bởi HR khi hẹn phỏng vấn

## 1. Pipeline chấm phù hợp

```
Input: rawCv (text từ PDF/DOCX) + JobRequirement (JD)
   │
   ▼
[1] Trích xuất text từ CV file (PdfPig / OpenXml)
   │
   ▼
[2] Anonymize: bỏ tên, SĐT, email, địa chỉ, tên trường/công ty cụ thể
   │  → AnonymizedCv (value object, compile-time guarantee)
   │
   ▼
[3] Tính CacheKey = SHA256(cvText, jdText, modelVersion, promptVersion)
   │  → Check cache. Có → trả ngay.
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
[5] Lưu AiScore (application_id, score, summary, adapter_used, cache_key)
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

`SimpleAnonymizer` phase 1 dùng regex:

- Email → `[EMAIL]`
- SĐT Việt Nam (`0\d{9,10}`, `+84\d{9,10}`) → `[PHONE]`
- Tên (regex đơn giản: 2-4 từ ghép, viết hoa chữ cái đầu, ở đầu CV) → `[NAME]`
- Địa chỉ (từ khóa "Địa chỉ:", "Address:") → `[ADDRESS]`
- Tên trường (từ khóa "Đại học", "University") → `[SCHOOL]` (giữ lại "chuyên ngành X" nếu có)
- Tên công ty cũ (khó tự động) → **HR duyệt lại tay khi thấy sai**

Sau phase 1, `SimpleAnonymizer` sẽ được thay bằng `LlmAnonymizer` — gọi LLM riêng để ẩn danh
chuẩn hơn (đây là fallback nếu regex không đủ, còn thời gian thì làm).

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

Cache lưu ở bảng `ai_scores` (không dùng Redis vì cần persist và join query). Trước khi gọi
LLM, luôn check `SELECT * FROM ai_scores WHERE cache_key = ?`.

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

- Ứng viên preview: 20 lượt/ngày (soft), 100 lượt/tuần (hard)
- HR khởi chạy lô: 5 lô/ngày, mỗi lô tối đa 500 hồ sơ
- Toàn hệ thống: budget alert khi vượt $25 tích lũy

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

## 6. Testing

- **Unit test**: mock `IAiScoringService` → `FakeAiScoringAdapter` trả điểm cố định (dùng cho
  test business logic ở Application layer). Test không tốn tiền, chạy < 1s.
- **Integration test**: gọi thật OpenAI trong CI chỉ trên nhánh `main` (env `OPENAI_API_KEY`),
  1-2 test smoke để chắc adapter chưa hỏng.
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
- Kiểu `AnonymizedCv` có constructor `internal` → chỉ `IAnonymizer` tạo được. Application layer
  không thể tự "cast" từ string sang AnonymizedCv.
- ArchitectureTests ép: mọi method trong `Ports/*ScoringService*` và `Ports/*QuestionGenerator*`
  chỉ nhận `AnonymizedCv`, không nhận `string` hay `Cv`.
- Prompt hệ thống có câu "Không được nêu tên riêng" — nếu LLM lỡ lộ tên qua Strengths/Gaps thì
  vẫn có tuyến ẩn danh ở tầng dưới, coi như defense in depth.
