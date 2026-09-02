# Quy ước làm việc

## 1. Nhánh

| Nhánh | Vai trò |
|---|---|
| `main` | Ổn định, đã deploy. Không commit trực tiếp. |
| `develop` | Tích hợp. Không commit trực tiếp. |
| `feature/*` | Từng việc. Tạo từ `develop`, merge lại vào `develop`. |

**Đặt tên nhánh kèm tầng** để nhìn là biết ai làm. Dùng **slug tiếng Anh, không dấu**, ngắn
gọn 3–5 từ:

```
feature/data-job-entity
feature/api-job-service
feature/web-job-list
feature/ai-cv-scoring
feature/contracts-job-dto
feature/docs-architecture
feature/docker-postgres-compose
```

Phạm vi hợp lệ ở đầu tên nhánh — **giống hệt** danh sách phạm vi của commit ở mục 2:
`data` | `business` | `api` | `web` | `ai` | `contracts` | `ci` | `docker` | `docs`

## 2. Commit

Theo Conventional Commits:

```
<loại>(<phạm vi>): <mô tả ngắn>

feat(api): them endpoint POST /api/jobs
fix(data): sua quan he Application - Candidate
docs(ai): bo sung prompt tom tat CV
test(business): them unit test state machine
chore(ci): them buoc dotnet test
```

Loại: `feat` | `fix` | `docs` | `test` | `refactor` | `chore`
Phạm vi: `data` | `business` | `api` | `web` | `ai` | `contracts` | `ci` | `docker` | `docs`

> `docs` vừa là *loại* vừa là *phạm vi*, và đó là chủ ý: `docs(api)` là sửa tài liệu của tầng
> API, còn `feat(docs)` thì vô nghĩa. Tuần 1–2 gần như toàn tài liệu nên phạm vi này cần có —
> thiếu nó thì không đặt tên nhánh tài liệu cho đúng quy ước được.

## 3. Pull Request — bắt buộc

**Mọi thay đổi vào `develop` và `main` đều phải đi qua Pull Request và có ít nhất một approve.
Không có ngoại lệ, kể cả sửa một dòng.**

Lý do không phải hình thức: tiêu chí *"Git, PR, review"* chiếm **15% điểm học phần**, và được
chấm bằng **minh chứng trên GitHub** — lịch sử PR, nội dung comment review, ai approve của ai.
Commit thẳng vào `develop` không để lại minh chứng nào, nên dù code có tốt thì phần điểm đó vẫn
mất. Trong 12 sản phẩm bàn giao cuối học phần, có hai mục là *"Lịch sử branch, commit, PR"* và
*"Minh chứng code review"*.

### 3.1. Vòng đời một PR

1. Tạo nhánh `feature/*` từ `develop`.
2. **Mở Draft PR ngay từ commit đầu tiên**, không đợi làm xong. Draft PR cho cả nhóm thấy việc
   đang chạy tới đâu, và cho CI chạy sớm để lỗi build lộ ra trong ngày đầu thay vì ngày cuối.
3. Làm xong → bấm *Ready for review*, gán reviewer theo bảng 3.3.
4. Reviewer phản hồi **trong vòng 24 giờ**.
5. Tác giả sửa theo góp ý, trả lời từng comment (không im lặng push đè).
6. Reviewer approve → **tác giả tự merge** (squash) và xoá nhánh.

### 3.2. Điều kiện merge

| Điều kiện | Ghi chú |
|---|---|
| CI xanh | `dotnet build` + `dotnet test` |
| ≥ 1 approve | PR chạm `src/ATS.Contracts` cần **2 approve** |
| Có liên kết issue | `Closes #12` trong phần mô tả |
| Không còn comment `[blocking]` chưa xử lý | Xem `docs/code-review.md` |

**Không có giới hạn số dòng.** PR to hay nhỏ là quyền của người làm; nhóm tin nhau tự cân nhắc.

**Cách merge:** `Squash and merge` khi gộp `feature/*` → `develop` (mỗi việc thành đúng một
commit sạch trên `develop`). `Merge commit` khi gộp `develop` → `main` (giữ lại lịch sử của
đợt phát hành).

### 3.3. Ai review PR của ai

Reviewer mặc định là **người tiêu thụ đầu ra của tầng bạn**, không phải người rảnh nhất. Lý do:
người dùng contract của bạn là người phát hiện sớm nhất nếu contract sai, và họ có động cơ thật
để đọc kỹ.

| Người mở PR | Tầng | Reviewer chính | Vì sao chọn người này |
|---|---|---|---|
| TV1 | `ATS.Data` | **TV2** | Business gọi repository — kiểu dữ liệu hoặc quan hệ sai sẽ đập vào TV2 trước tiên |
| TV2 | `ATS.Business`, `ATS.Api` | **TV3** | Web gọi API — DTO thiếu trường, mã lỗi khó xử lý sẽ lộ ra ở phía TV3 |
| TV3 | `ATS.Web` | **TV2** | TV2 biết API trả gì, phát hiện được chỗ giao diện dùng sai hợp đồng |
| TV4 | `ATS.AI`, `ATS.Tests` | **TV1** | TV1 là người dự phòng mảng AI (chống bus factor = 1) nên buộc phải đọc code AI đều đặn |
| Bất kỳ | `ATS.Contracts` | **2 người** thuộc hai tầng bị ảnh hưởng | Thay đổi contract ảnh hưởng mọi người |

Reviewer chính bận quá 24 giờ → **người dự phòng** của mảng đó approve thay, và ghi rõ lý do
ngay trong PR (`TV2 nghỉ ốm, TV1 review thay`). Việc không được đứng chờ, nhưng cũng không được
merge chui.

File [.github/CODEOWNERS](.github/CODEOWNERS) tự động gán reviewer theo bảng này — không ai phải
nhớ.

### 3.4. Tách việc lớn — gợi ý, không bắt buộc

Nhóm **không đặt giới hạn kích thước PR**. Mỗi người tự quyết định mở PR lúc nào và to cỡ nào.

Gợi ý cho lúc thấy PR của mình bắt đầu khó review: tách theo **bước dọc trong một tầng**, mỗi
bước một PR. Đây là gợi ý để tham khảo, không phải quy tắc phải theo:

```
feature/data-job-entity        → entity + cấu hình EF
feature/data-job-migration     → migration + seed
feature/data-job-repository    → repository + unit test
feature/api-job-service        → service nghiệp vụ + test
feature/api-job-controller     → controller + Swagger
feature/web-job-list           → màn hình danh sách
```

### 3.5. Ngoại lệ duy nhất

PR **chỉ sửa tài liệu** (`docs/`, `*.md`): vẫn mở PR, vẫn cần 1 approve, nhưng bỏ yêu cầu viết test.

Không có ngoại lệ nào khác. Đặc biệt **không có ngoại lệ "sửa gấp trước buổi demo"** — đó chính
là lúc dễ đẩy lỗi vào `main` nhất.

### 3.6. Bật branch protection trên GitHub (TV1 làm, tuần 1)

Quy ước chỉ là quy ước cho tới khi GitHub cưỡng chế nó. `Settings → Branches → Add rule` cho cả
`main` và `develop`:

- [x] Require a pull request before merging
- [x] Require approvals — **1** (đặt **2** cho `main`)
- [x] Dismiss stale pull request approvals when new commits are pushed
- [x] Require status checks to pass before merging → chọn check **`build-and-test`**
- [x] Require branches to be up to date before merging
- [x] Require conversation resolution before merging
- [ ] ~~Allow force pushes~~ / ~~Allow deletions~~ — để tắt

> **Lưu ý về gói tài khoản:** trên gói GitHub Free, branch protection chỉ áp dụng cho repo
> **public**; repo private cần gói Pro/Team. Hai cách xử lý: **để repo public** (đồ án học phần
> không có gì bí mật, lại tiện cho giảng viên xem), hoặc dùng **GitHub Student Developer Pack**
> để có Pro miễn phí. Kiểm tra lại trong `Settings → Branches` vì chính sách gói có thể thay đổi.

### 3.7. Thay đổi `src/ATS.Contracts`

Ngoài yêu cầu 2 approve: **báo cả nhóm trước khi mở PR**, và ghi rõ trong PR những tầng nào phải
sửa theo. Đây là loại thay đổi duy nhất có thể làm cả ba người còn lại phải dừng việc.

## 4. Ranh giới sở hữu

| Thư mục | Người sở hữu |
|---|---|
| `src/ATS.Data`, `docker/`, `.github/` | TV1 |
| `src/ATS.Business`, `src/ATS.Api` | TV2 |
| `src/ATS.Web` | TV3 |
| `src/ATS.AI`, `src/ATS.Tests` | TV4 |
| `src/ATS.Contracts` | Cả nhóm |

Muốn sửa file thuộc tầng người khác → báo và thống nhất với chủ tầng đó trước khi sửa.

Bảng này được mã hoá trong [.github/CODEOWNERS](.github/CODEOWNERS): GitHub tự động gán chủ tầng
làm reviewer khi PR chạm vào thư mục của họ.

## 5. Contract-first

Các tầng giao tiếp qua DTO + interface trong `src/ATS.Contracts`, **chốt trong tuần 2**.
Sau đó mỗi người phát triển song song trên **mock/stub** của tầng dưới — không ai ngồi chờ ai.

Ví dụ: TV3 dựng toàn bộ giao diện trên `MockJobService` trước khi TV2 viết xong `JobService`.

## 6. Migration

- Chỉ **TV1** tạo và duyệt migration.
- Một thời điểm chỉ có **một** migration đang mở, để tránh xung đột.
- Đặt tên: `20260301_AddJobEntity`.

## 7. Kiểm thử

- Người viết code là người viết unit test cho code đó.
- **Code mới không có test → chưa gộp vào `develop`** (trừ thay đổi chỉ sửa tài liệu).
- TV4 phụ trách test tích hợp và E2E.

## 8. Bảo mật

- Không commit API key, connection string thật, hay dữ liệu ứng viên thật.
- Dùng `dotnet user-secrets` khi phát triển, biến môi trường khi deploy.
- Dữ liệu nhạy cảm (CCCD, SĐT, email) phải được **ẩn danh trước khi gửi tới dịch vụ AI**.

## 9. Nhật ký công việc

Cuối mỗi tuần, mỗi người cập nhật file của mình trong `docs/worklog/`.
Đây là minh chứng đóng góp cá nhân khi chấm điểm — không cập nhật là mất điểm.
