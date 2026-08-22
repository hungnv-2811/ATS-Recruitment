# Contract giữa các tầng — `ATS.Contracts`

> **Đây là tài liệu quan trọng nhất của tuần 2.** Chưa chốt xong contract thì không ai được
> sang tuần 3.

## Vì sao cần

Nhóm chia việc theo tầng, nên một chức năng đi qua tay 3 người. Nếu ai cũng chờ tầng dưới xong
mới làm được thì cả nhóm tắc. Contract-first giải quyết: chốt trước **DTO + interface**, sau đó
mỗi người code song song trên **mock/stub** của tầng dưới.

```
TV3 (Web)  ──gọi──►  IJobService   ◄──cài đặt──  TV2 (Business)
TV2        ──gọi──►  IJobRepository◄──cài đặt──  TV1 (Data)
TV2        ──gọi──►  IAiScoringService ◄──cài đặt──  TV4 (AI)
```

Khi TV2 chưa viết xong `JobService`, TV3 vẫn dựng được giao diện trên `MockJobService`.

## Quy tắc sửa contract

1. Mở **PR riêng** chỉ động vào `src/ATS.Contracts`.
2. Cần **2 approve từ 2 tầng khác nhau**.
3. Ghi rõ trong mô tả PR: tầng nào bị ảnh hưởng, ai cần sửa theo.

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

### 6. AI

- [ ] `CvSummaryDto` — tóm tắt CV do LLM sinh
- [ ] `MatchScoreDto` — điểm 0–100 + lý do + thời điểm chấm
- [ ] `RankedCandidateDto` — ứng viên kèm điểm và thứ hạng
- [ ] `IAiScoringService` — `ScoreAsync(cvText, jdText)`, `SummarizeAsync(cvText)`
- [ ] `IAiScoringService` phải định nghĩa rõ **hành vi khi lỗi** (trả về null / trạng thái
      `Unavailable`), để tầng trên hiển thị đúng cho HR

## Quy ước đặt tên

| Loại | Quy ước | Ví dụ |
|---|---|---|
| DTO trả ra | `<Tên>Dto` | `JobDto` |
| Tham số vào | `<Tên><Hành động>Request` | `JobCreateRequest` |
| Interface service | `I<Tên>Service` | `IJobService` |
| Interface repository | `I<Tên>Repository` | `IJobRepository` |
| Enum trạng thái | danh từ số ít | `ApplicationStatus` |

**Không** đưa entity của EF Core ra ngoài tầng Data — tầng trên chỉ thấy DTO.
