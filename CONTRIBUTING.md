# Quy ước làm việc

## 1. Nhánh

| Nhánh | Vai trò |
|---|---|
| `main` | Ổn định, đã deploy. Không commit trực tiếp. |
| `develop` | Tích hợp. Không commit trực tiếp. |
| `feature/*` | Từng việc. Tạo từ `develop`, merge lại vào `develop`. |

**Đặt tên nhánh kèm tầng** để nhìn là biết ai làm:

```
feature/data-job-entity
feature/api-job-service
feature/web-job-list
feature/ai-cv-scoring
feature/contracts-job-dto
```

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
Phạm vi: `data` | `business` | `api` | `web` | `ai` | `contracts` | `ci` | `docker`

## 3. Pull Request

- Mở PR từ `feature/*` vào `develop`.
- **Bắt buộc ít nhất 1 approve** từ thành viên khác trước khi merge.
- PR phải liên kết tới issue (`Closes #12`).
- Điền đủ checklist trong template PR.
- PR sửa `src/ATS.Contracts` cần **2 approve từ 2 tầng khác nhau**, vì nó ảnh hưởng mọi người.

## 4. Ranh giới sở hữu

| Thư mục | Người sở hữu |
|---|---|
| `src/ATS.Data`, `docker/`, `.github/` | TV1 |
| `src/ATS.Business`, `src/ATS.Api` | TV2 |
| `src/ATS.Web` | TV3 |
| `src/ATS.AI`, `src/ATS.Tests` | TV4 |
| `src/ATS.Contracts` | Cả nhóm |

Muốn sửa file thuộc tầng người khác → mở PR riêng và cần approve của chủ tầng đó.

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
- **PR không có test → không merge** (trừ PR chỉ sửa tài liệu).
- TV4 phụ trách test tích hợp và E2E.

## 8. Bảo mật

- Không commit API key, connection string thật, hay dữ liệu ứng viên thật.
- Dùng `dotnet user-secrets` khi phát triển, biến môi trường khi deploy.
- Dữ liệu nhạy cảm (CCCD, SĐT, email) phải được **ẩn danh trước khi gửi tới dịch vụ AI**.

## 9. Nhật ký công việc

Cuối mỗi tuần, mỗi người cập nhật file của mình trong `docs/worklog/`.
Đây là minh chứng đóng góp cá nhân khi chấm điểm — không cập nhật là mất điểm.
