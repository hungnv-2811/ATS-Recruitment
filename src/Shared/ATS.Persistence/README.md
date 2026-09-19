# ATS.Persistence

`AtsDbContext` dùng chung cho cả 3 module + toàn bộ migration.

## Vì sao là project riêng

Một DbContext dùng chung cần một chỗ ở chung. Ba phương án đã cân nhắc:

| Phương án | Vì sao loại |
|---|---|
| Đặt trong `Recruitment.Infrastructure` | Hai module kia phải reference chéo — vi phạm ranh giới module |
| Đặt trong `ATS.SharedKernel` | Kéo EF Core vào SharedKernel, mà mọi Domain đều reference SharedKernel → Domain gián tiếp phụ thuộc EF, vi phạm quy tắc kiến trúc số 1 |
| Project riêng | **Đã chọn.** Chỉ 3 project Infrastructure reference nó; Domain không thấy nó |

Xem `docs/architecture.md` mục 3.2.

## Cấu hình entity KHÔNG nằm ở đây

Mỗi module tự viết `IEntityTypeConfiguration<T>` trong Infrastructure của mình. Composition
Root đăng ký assembly của module với `AtsDbContextConfigurator`, và `AtsDbContext` nạp qua
`ApplyConfigurationsFromAssembly`.

Nhờ vậy `AtsDbContext` **không tham chiếu project của module nào** — phụ thuộc đi đúng chiều.

```csharp
// ATS.Api/Program.cs — Composition Root
AtsDbContextConfigurator.Register(typeof(ATS.Recruitment.Infrastructure.InfrastructureAssemblyMarker).Assembly);
```

## Ba schema

| Schema | Nội dung |
|---|---|
| `identity` | `users`, `password_reset_tokens` — chỉ phục vụ xác thực |
| `recruitment` | `candidates`, `hr_profiles`, `cvs`, `jobs`, `applications`, `interviews`, `evaluations` |
| `aiscreening` | `ai_scores`, `ai_score_cache`, `screening_jobs`, `interview_questions`, `ai_usage_quotas` |

## Tạo migration

```bash
dotnet tool restore
dotnet dotnet-ef migrations add TenMigration --project src/Shared/ATS.Persistence
```

Không cần `--startup-project`: `AtsDbContextFactory` (design-time) đã lo phần đó, nên tạo
migration không phải kéo `Microsoft.EntityFrameworkCore.Design` vào `ATS.Api`.

Xem SQL trước khi chạy:

```bash
dotnet dotnet-ef migrations script --project src/Shared/ATS.Persistence --idempotent
```

## Trạng thái hiện tại

`20260919131605_InitialCreate` — **chỉ tạo 3 schema, chưa có bảng nào**. Đúng theo kế hoạch
tuần 2. Bảng thật bắt đầu từ tuần 3 (`identity.users`).
