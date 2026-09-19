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
Root **truyền thẳng** danh sách assembly vào lúc đăng ký, và `AtsDbContext` nạp qua
`ApplyConfigurationsFromAssembly`.

Nhờ vậy `AtsDbContext` **không tham chiếu project của module nào** — phụ thuộc đi đúng chiều.

```csharp
// ATS.Api/Program.cs — Composition Root
builder.Services.AddAtsPersistence(
    connectionString,
    typeof(ATS.Recruitment.Infrastructure.InfrastructureAssemblyMarker).Assembly,
    typeof(ATS.AiScreening.Infrastructure.InfrastructureAssemblyMarker).Assembly,
    typeof(ATS.Identity.Infrastructure.InfrastructureAssemblyMarker).Assembly);
```

`AddAtsPersistence` là **cách duy nhất** đăng ký `AtsDbContext`, và nó bắt buộc nhận danh
sách assembly. Nên không tồn tại trạng thái "đã tạo DbContext nhưng chưa đăng ký module" —
quên thì hỏng ngay lúc khởi động, không phải âm thầm mất bảng.

## Ba schema

| Schema | Nội dung |
|---|---|
| `identity` | `users`, `password_reset_tokens` — chỉ phục vụ xác thực |
| `recruitment` | `candidates`, `hr_profiles`, `cvs`, `jobs`, `applications`, `interviews`, `evaluations` |
| `aiscreening` | `ai_scores`, `ai_score_cache`, `screening_jobs`, `interview_questions`, `ai_usage_quotas` |

## Tạo migration

```bash
dotnet tool restore
dotnet dotnet-ef migrations add TenMigration \
  --project src/Shared/ATS.Persistence \
  --startup-project src/Hosts/ATS.Api
```

`--startup-project` là **bắt buộc**, không phải tuỳ chọn. EF dựng model bằng chính
Composition Root của `ATS.Api`, nên danh sách module lúc sinh migration và lúc chạy
luôn giống nhau. Bỏ nó đi (hoặc dùng một design-time factory riêng) thì EF dựng model
với **danh sách module rỗng** và sinh ra migration trống — build xanh, CI xanh, chỉ vỡ
lúc chạy.

Xem SQL trước khi chạy:

```bash
dotnet dotnet-ef migrations script --project src/Shared/ATS.Persistence --idempotent
```

## Trạng thái hiện tại

`20260919131605_InitialCreate` — **chỉ tạo 3 schema, chưa có bảng nào**. Đúng theo kế hoạch
tuần 2. Bảng thật bắt đầu từ tuần 3 (`identity.users`).
