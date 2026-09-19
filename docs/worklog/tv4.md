# Worklog — TV4 — AI & Chất lượng

| | |
|---|---|
| **Họ tên** | Tiền |
| **MSSV** | (điền) |
| **GitHub** | (điền) |
| **Tầng phụ trách** | AI & Chất lượng |
| **Project sở hữu** | `ATS.AiScreening.*`, `src/Tests/` |

> Mẫu ghi: [_TEMPLATE.md](_TEMPLATE.md). Tuần mới nhất đặt **trên cùng**.

> **Lưu ý khi nộp:** các ô `#—` (Issue/PR) và `~—h` (thời gian) là phần **mỗi người tự điền**
> theo hoạt động thật của mình trên GitHub. Phần "Việc đã làm" ghi đúng trạng thái repo.


---

## Tuần 2 (dd/mm – dd/mm) — Dựng khung

**Chức năng chung của tuần:** `docker compose up` chạy được, ArchitectureTests xanh.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| `ATS.ArchitectureTests`: 5 quy tắc / 9 test, reflection thuần (bỏ NetArchTest) | #— | #— | Xong |
| Kiểm chứng test bằng cách **cố tình phá luật** → đúng 2 test đỏ, 7 xanh | #— | #— | Xong |
| `AnonymizedCv` (ctor `internal`) + `IAnonymizer` + `SimpleAnonymizer` trong Domain | #— | #— | Xong |
| `IPiiRedactor` — đường đi cho anonymizer LLM ở phase 2 mà không phá ADR-3 | #— | #— | Xong |
| Port AI: `IAiScoringService`, `IInterviewQuestionGenerator`, `IAiScoreCacheRepository`, `IAiUsageQuota` | #— | #— | Xong |
| 13 unit test cho anonymizer, `CacheKey`, `Score` | #— | #— | Xong |

### Vướng mắc

- Quy tắc 3 (Controller không đụng `DbContext`) hiện còn **rỗng** vì tuần 2 chưa có controller
  nào. Phải rà lại ở tuần 3 khi `ATS.Api` có controller đầu tiên, nếu không nó pass mà không
  kiểm gì cả.

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

-

### Kế hoạch tuần sau

-

**Thời gian bỏ ra:** ~—h

---

## Tuần 1 (dd/mm – dd/mm) — Khởi động

**Chức năng chung của tuần:** phân tích yêu cầu, chốt phạm vi & kiến trúc, khởi tạo repo.

### Việc đã làm

| Việc | Issue | PR | Trạng thái |
|---|---|---|---|
| Nghiên cứu pipeline AI, đăng ký API key | #— | #— | Chưa bắt đầu |

### PR đã review của người khác

| PR | Của ai | Nhận xét chính |
|---|---|---|
| #— | | |

### Vướng mắc

-

### Kế hoạch tuần sau

-

**Thời gian bỏ ra:** ~—h

---
