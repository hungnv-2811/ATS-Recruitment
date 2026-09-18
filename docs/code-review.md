# Code Review Checklist

> Áp dụng cho mọi PR. Reviewer đánh dấu ✓ hoặc để lại comment cho từng mục liên quan.

## 1. Ranh giới kiến trúc

- [ ] Domain **không** reference Infrastructure hay bất kỳ SDK/framework nào (chỉ SharedKernel + BCL)
- [ ] Application dùng port qua DI, **không** `new` implementation cụ thể
- [ ] Controllers **không** đụng `DbContext` trực tiếp — luôn qua Application layer
- [ ] Không có module A reference project của module B
- [ ] Cross-module call đi qua interface đặt ở bên "chủ động gọi" (ví dụ `Recruitment.Application.IApplicationScreeningTrigger`)

## 2. PII và bảo mật (RB3, RB8)

- [ ] Nếu PR đụng AI: mọi method AI chỉ nhận `AnonymizedCv`, **không** `string` hay `Cv` thô
- [ ] Không log CV text, tên ứng viên, SĐT, email
- [ ] Nếu PR đụng phân quyền: Ứng viên chỉ đọc/ghi dữ liệu của mình (`candidate_id = current_user.candidate_id`)
- [ ] Nếu PR đụng phân quyền: HR chỉ thao tác trên tin mà mình là owner
- [ ] Không có endpoint nào trả về `password_hash` hay token trong response body

## 3. Domain modeling

- [ ] Entity có identity (Id), Value Object không có Id và immutable
- [ ] Constructor entity **private** hoặc **factory method** — không cho new bừa
- [ ] Invariant được kiểm tra trong constructor/setter, không phải ở Application
- [ ] State machine (`Application`) chỉ thay đổi qua method có tên rõ ràng (`MoveToScreening()`)
- [ ] Không có setter public cho property nghiệp vụ

## 4. Async & CancellationToken

- [ ] Mọi method I/O đều `async` + hậu tố `Async`
- [ ] `CancellationToken` là tham số **cuối cùng** của method async
- [ ] Không `.Result` / `.Wait()` — luôn `await`
- [ ] Không quên truyền `CancellationToken` xuống dưới

## 5. EF Core

- [ ] Không trả `IQueryable` ra khỏi Repository
- [ ] `AsNoTracking()` cho query chỉ đọc
- [ ] Không N+1: `Include` khi cần, hoặc `Select` DTO
- [ ] Migration có tên rõ ràng (`20260920_AddCvIsDefault`)
- [ ] Không sửa migration đã merge vào main — luôn tạo migration mới

## 6. Testing

- [ ] Handler nghiệp vụ có unit test (mock repo, mock port)
- [ ] Test chạy < 5 giây tổng thể (nếu chậm hơn: có thể có test đang gọi mạng thật)
- [ ] Test không đụng OpenAI thật (dùng `FakeAiScoringAdapter`)
- [ ] Test không đụng database thật (dùng in-memory hoặc Testcontainers cho integration test)

## 7. API design

- [ ] Endpoint follow REST convention: `POST /resources`, `GET /resources/{id}`, không `POST /doSomething`
- [ ] Trả HTTP status đúng: 200 OK, 201 Created, 202 Accepted (cho async), 400 Bad Request, 404, 409 Conflict
- [ ] Error response có shape thống nhất: `{ "error": "...", "details": [...] }`
- [ ] Không leak stack trace ra client
- [ ] Có validation cho input (FluentValidation hoặc DataAnnotations)

## 8. Frontend (Blazor)

- [ ] Không gọi API trực tiếp từ Razor page — qua service class
- [ ] Loading state cho request > 500ms
- [ ] Error handling: hiện toast/alert, không crash trắng màn
- [ ] Component có thể tái sử dụng đặt trong `Shared/`
- [ ] Không hardcode URL — dùng config

## 9. Naming & readability

- [ ] Tên class/method đọc ra ngữ nghĩa: `ScreeningTriggerAdapter` chứ không `Adapter1`
- [ ] Không comment thừa (`// increment i by 1`)
- [ ] Method dài > 40 dòng: cân nhắc tách
- [ ] File > 300 dòng: cân nhắc tách
- [ ] Không tiếng Việt trong tên biến/class, chỉ tiếng Việt trong comment giải thích nghiệp vụ phức tạp

## 10. Git hygiene

- [ ] Commit message follow convention (`feat: add cv upload`, `fix: null ref in ...`, xem `CONTRIBUTING.md`)
- [ ] PR mô tả: **What** + **Why**, không chỉ paste diff
- [ ] Không commit file bí mật (`.env`, `appsettings.Development.json` với API key)
- [ ] Không commit `bin/`, `obj/`, `node_modules/`, file upload người dùng

## 11. Trước khi bấm Merge

- [ ] CI xanh (build + test + ArchitectureTests)
- [ ] Ít nhất 1 approval
- [ ] Không có `TODO:` chưa xử lý ở phần nghiệp vụ chính (TODO ở polish thì OK, ghi issue)
- [ ] Reviewer đã đọc code, không chỉ approve theo phong trào

---

## Mẫu comment thường dùng

- **nit:** Không quan trọng, có thể sửa hoặc bỏ qua
- **suggestion:** Đề xuất cải tiến nhưng không chặn merge
- **question:** Hỏi để hiểu, chưa chắc là vấn đề
- **must:** Phải sửa trước khi merge
- **arch:** Vấn đề ranh giới kiến trúc — phải sửa
- **security:** Vấn đề bảo mật — phải sửa

## Ví dụ

```
[must] Method `ScoreAsync` nhận `string cvText` — theo ADR-3 phải là `AnonymizedCv`.
       Sửa signature và thêm test.

[arch] `JobController` đang inject `AtsDbContext` trực tiếp. Chuyển sang `IJobService`
       ở Application layer.

[suggestion] Có thể extract phần retry logic thành `RetryPolicy` để dùng lại ở
             `OpenAiInterviewQuestionAdapter`.
```
