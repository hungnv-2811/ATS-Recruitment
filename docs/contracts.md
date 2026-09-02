# Contract giữa các tầng — `ATS.Contracts`

> **Đây là tài liệu quan trọng nhất của tuần 2.** Chưa chốt xong contract thì không ai được
> sang tuần 3.

## Vì sao cần

Nhóm chia việc theo tầng, nên một chức năng đi qua tay 3 người. Nếu ai cũng chờ tầng dưới xong
mới làm được thì cả nhóm tắc. Contract-first giải quyết: chốt trước **DTO + interface**, sau đó
mỗi người code song song trên **mock/stub** của tầng dưới.

```
TV3 (Web)  ──gọi──►  IJobService        ◄──cài đặt──  TV2 (Business)
TV2        ──gọi──►  IJobRepository     ◄──cài đặt──  TV1 (Data)
TV2        ──gọi──►  IScreeningQueue    ◄──cài đặt──  TV1 (hạ tầng Redis)
TV4        ──gọi──►  IAiScoringService  ◄──cài đặt──  TV4 (các adapter)
```

Khi TV2 chưa viết xong `JobService`, TV3 vẫn dựng được giao diện trên `MockJobService`.

> **Lưu ý về chiều gọi của AI, đã đổi so với bản đầu:** tầng nghiệp vụ **không** gọi
> `IAiScoringService`. Module AI screening chỉ đẩy việc qua `IScreeningQueue` rồi trả `202`;
> **worker** mới là bên gọi `IAiScoringService` (`architecture.md` — ADR-02). Hệ quả về mặt
> tham chiếu project: **`ATS.Api` và `ATS.Business` không được tham chiếu `ATS.AI`** — chỉ tiến
> trình worker mới tham chiếu. Đây là cách để trình biên dịch bảo đảm API không bao giờ gọi
> thẳng LLM, và nó được kiểm bằng test ranh giới trong CI.

## Quy tắc sửa contract

1. Tách riêng một commit/nhánh chỉ động vào `src/ATS.Contracts`, không trộn với việc khác.
2. **Báo cả nhóm trước khi gộp** — ít nhất chủ của các tầng bị ảnh hưởng phải biết.
3. Ghi rõ trong mô tả: tầng nào bị ảnh hưởng, ai cần sửa theo.

## Danh sách contract cần chốt

### 1. Tài khoản & phân quyền

- [ ] `UserDto`, `LoginRequest`, `LoginResponse`, `RegisterRequest`
- [ ] `IAuthService`, `IUserRepository`

### 2. Tin tuyển dụng (Job/JD)

- [ ] `JobDto`, `JobCreateRequest`, `JobUpdateRequest`, `JobListQuery`
- [ ] `IJobService`, `IJobRepository`

### 3. Ứng viên & CV

- [ ] `CandidateDto`, `CvDto`, `CvUploadRequest`, `CvTextResult`
- [ ] `ICandidateService`, `ICvService`, `ICandidateRepository`, `ICvRepository`

### 4. Quy trình tuyển dụng

- [ ] `ApplicationDto`, `ApplicationStatus` (enum), `StatusChangeRequest`, `InterviewDto`
- [ ] `IApplicationService`, `IInterviewService`, `IApplicationRepository`

### 5. Báo cáo & thống kê

- [ ] `RecruitmentStatsDto`, `JobFunnelDto`, `ReportQuery`
- [ ] `IReportService`

### 6. AI — sàng lọc bất đồng bộ

**DTO hiển thị:**

- [ ] `CvSummaryDto` — tóm tắt CV do LLM sinh
- [ ] `MatchScoreDto` — điểm 0–100 + lý do + thời điểm chấm + `SummaryStatus`
- [ ] `RankedCandidateDto` — ứng viên kèm điểm và thứ hạng
- [ ] `ScreeningRunDto` — trạng thái một lượt sàng lọc lô: `Queued`/`Running`/`Done`/`Failed`,
      số CV đã xong trên tổng số (để UI hiển thị `180/300`)

**Kiểu của pipeline AI** — bốn kiểu này là phần dễ bị bỏ sót nhất khi chốt contract:

- [ ] `AnonymizedCv` — CV **đã qua bước ẩn danh**. Là một kiểu riêng chứ không phải `string`:
      xem ghi chú dưới đây.
- [ ] `JobRequirement` — JD ở dạng đã chuẩn hoá để so khớp
- [ ] `ScreeningResult` — điểm + tóm tắt + lý do + `CacheKey` + `ModelVersion` + `PromptVersion`
- [ ] `ScreeningTask` — thông điệp đẩy vào hàng đợi (`cvId`, `jobId`, `runId`, `correlationId`)

**Interface:**

- [ ] `IScreeningQueue` — `EnqueueAsync(ScreeningTask task, CancellationToken ct)`.
      Tầng nghiệp vụ chỉ biết interface này, không biết Redis. Đổi sang RabbitMQ là đổi adapter.
- [ ] `IAiScoringService` — `ScoreAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct)`
      ⚠️ Tham số **phải là kiểu `AnonymizedCv`, không được là `string`**: đó là cách bắt trình
      biên dịch chặn việc gửi CV thô ra ngoài (`architecture.md` — ADR-03). Ai đề xuất đổi về
      `string` cho tiện thì đọc lại RB8 trước.
- [ ] `IAiScoringService` phải định nghĩa rõ **hành vi khi lỗi**: phân biệt lỗi **tạm thời**
      (đáng retry) với lỗi **vĩnh viễn** (vào dead-letter), và trả `SummaryStatus = Unavailable`
      khi chỉ có điểm mà không có tóm tắt — để tầng trên hiển thị đúng cho HR
- [ ] `IFileStorage` — `SaveAsync` / `OpenReadAsync` cho file CV. Có interface này thì đổi từ
      ổ đĩa sang S3/Blob về sau không phải sửa nghiệp vụ (`architecture.md` mục 3.4)

## Quy ước đặt tên

| Loại | Quy ước | Ví dụ |
|---|---|---|
| DTO trả ra | `<Tên>Dto` | `JobDto` |
| Tham số vào | `<Tên><Hành động>Request` | `JobCreateRequest` |
| Interface service | `I<Tên>Service` | `IJobService` |
| Interface repository | `I<Tên>Repository` | `IJobRepository` |
| Enum trạng thái | danh từ số ít | `ApplicationStatus` |

**Không** đưa entity của EF Core ra ngoài tầng Data — tầng trên chỉ thấy DTO.
