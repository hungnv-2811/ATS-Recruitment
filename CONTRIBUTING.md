# Quy ước làm việc

## 1. Nhánh

| Nhánh | Vai trò |
|---|---|
| `main` | Ổn định, đã deploy. Không commit trực tiếp. |
| `nvhung` | Tích hợp. Không commit trực tiếp. |
| `feature/*` | Từng việc. Tạo từ `nvhung`, merge lại vào `nvhung`. |

**Đặt tên nhánh kèm phạm vi** để nhìn là biết ai làm. Dùng **slug tiếng Anh, không dấu**, ngắn
gọn 3–5 từ:

```
feature/recruitment-job-entity
feature/recruitment-cv-upload
feature/aiscreening-openai-adapter
feature/aiscreening-fallback-pipeline
feature/identity-jwt-login
feature/api-application-endpoint
feature/web-candidate-dashboard
feature/shared-entity-base
feature/docs-architecture
feature/docker-compose-update
feature/ci-arch-tests
```

Phạm vi hợp lệ ở đầu tên nhánh — **giống hệt** danh sách phạm vi của commit ở mục 2:
`recruitment` | `aiscreening` | `identity` | `shared` | `api` | `worker` | `web` | `contracts` | `ci` | `docker` | `docs` | `test`

## 2. Commit

Theo Conventional Commits:

```
<loại>(<phạm vi>): <mô tả ngắn>

feat(api): them endpoint POST /api/applications
feat(aiscreening): them OpenAiScoringAdapter
fix(recruitment): sua quan he Application - Cv
feat(identity): them role Candidate va HR
docs(architecture): cap nhat ADR-3 ve 2 port AI
test(aiscreening): them snapshot test cho scoring
chore(ci): them buoc chay ArchitectureTests
```

Loại (bắt buộc): `feat` | `fix` | `docs` | `test` | `refactor` | `chore` | `perf` | `style`

Ngôn ngữ commit: **tiếng Việt không dấu** để tránh lỗi encoding trên các máy khác nhau.

## 3. Pull Request

- PR từ `feature/*` → `nvhung`. PR từ `nvhung` → `main` chỉ khi có release.
- Tiêu đề PR: `<loại>(<phạm vi>): <mô tả>` giống commit
- Body PR có 2 phần: **What** (làm gì) + **Why** (vì sao)
- Screenshot nếu là UI, curl command nếu là API
- Link tới issue nếu có

### PR nhỏ, không PR khổng lồ

- < 300 dòng diff: tốt
- 300–800: cần lý do (ví dụ scaffolding ban đầu)
- > 800: **tách ra**, trừ trường hợp đặc biệt

## 4. Review

Xem `docs/code-review.md` cho checklist chi tiết.

- Cần **ít nhất 1 approval** trước khi merge
- CI phải xanh (build + test + ArchitectureTests)
- PR đụng ArchitectureTests → **TV4** review bắt buộc
- PR đụng migration → **TV1** review bắt buộc
- PR đụng port AI → **TV4** review bắt buộc

## 5. Cấu trúc thư mục

```
src/
├── Shared/
│   ├── ATS.SharedKernel/
│   └── ATS.Persistence/
├── Modules/
│   ├── Recruitment/
│   │   ├── ATS.Recruitment.Domain/
│   │   ├── ATS.Recruitment.Application/
│   │   └── ATS.Recruitment.Infrastructure/
│   ├── AiScreening/
│   │   ├── ATS.AiScreening.Domain/
│   │   ├── ATS.AiScreening.Application/
│   │   └── ATS.AiScreening.Infrastructure/
│   └── Identity/
│       ├── ATS.Identity.Domain/
│       ├── ATS.Identity.Application/
│       └── ATS.Identity.Infrastructure/
├── Hosts/
│   ├── ATS.Api/
│   ├── ATS.Worker/
│   └── ATS.Web/
└── Tests/
    ├── ATS.AiScreening.Tests/
    ├── ATS.Recruitment.Tests/
    ├── ATS.ArchitectureTests/
    └── ATS.IntegrationTests/
```

## 6. Quy tắc kiến trúc (bắt buộc)

Được ép bằng `ATS.ArchitectureTests` trong CI. Vi phạm → CI đỏ:

1. **Domain không reference Infrastructure.**
2. **Port AI (`IAiScoringService`, `IInterviewQuestionGenerator`) chỉ nhận `AnonymizedCv`, không `string` hay `Cv`.**
   `AnonymizedCv`, `IAnonymizer` và `SimpleAnonymizer` sống trong `AiScreening.Domain`;
   constructor `internal` nên **không assembly nào khác tạo được**. Module `Recruitment`
   không được tham chiếu `AnonymizedCv` (xem ADR-3).
3. **Controllers không được inject `DbContext`, phải qua Application service.**

## 7. Chạy local

```bash
# 1. Clone
git clone https://github.com/hungnv-2811/ATS-Recruitment.git
cd ATS-Recruitment

# 2. Copy env mẫu
cp docker/.env.example docker/.env
# Sửa docker/.env: điền OPENAI_API_KEY nếu có (không bắt buộc, Fake adapter chạy được)

# 3. Chạy
docker compose -f docker/docker-compose.yml up -d

# 4. Kiểm tra
curl http://localhost:8080/health
```

### Hai file `.env.example`, dùng file nào

| File | Dùng khi | Ai đọc |
|---|---|---|
| `docker/.env.example` → `docker/.env` | Chạy bằng Docker Compose | `docker compose` tự nạp `.env` **nằm cạnh file compose** |
| `.env.example` → `.env` | Chạy `dotnet run` trực tiếp trên máy | `DotNetEnv` / user-secrets |

`docker compose -f docker/docker-compose.yml` **không** đọc `.env` ở thư mục gốc — nó đọc
`docker/.env`. Đặt khoá vào đúng file, nếu không container sẽ chạy với biến rỗng mà không báo
lỗi gì.

| Cổng mặc định | Dịch vụ | Biến để đổi |
|---|---|---|
| 8080 | API + Swagger | `API_PORT` |
| 8081 | Web (Blazor) | `WEB_PORT` |
| 8025 | MailHog UI | `MAILHOG_UI_PORT` |
| 1025 | MailHog SMTP | `MAILHOG_SMTP_PORT` |
| 5432 | PostgreSQL | `DB_PORT` |
| 6379 | Redis | `REDIS_PORT` |

**Cổng bị dự án khác chiếm?** Đặt biến tương ứng trong `docker/.env`, **đừng sửa
`docker-compose.yml`** — sửa file đó là cả nhóm phải sửa theo.

```bash
# vi du: may dang chay san thu khac o 8080
echo "API_PORT=18080" >> docker/.env
docker compose -f docker/docker-compose.yml up -d
```

Triệu chứng của xung đột cổng rất dễ đánh lừa: `curl localhost:8080/health` trả về một trang
lạ (302, trang đăng nhập của ứng dụng khác) thay vì lỗi "connection refused" — vì **có** thứ
đang lắng nghe ở đó, chỉ không phải API của mình. Kiểm tra bằng `docker ps` xem cột `PORTS`
của `docker-api-1` có thật sự ánh xạ ra host không.

## 8. Trước khi push

```bash
dotnet tool restore   # lần đầu clone: cài dotnet-ef đúng version của nhóm
dotnet build          # phải xanh — cảnh báo bị coi là lỗi
dotnet test           # phải xanh (bao gồm 5 quy tắc ArchitectureTests)
dotnet format         # định dạng code
```

### Hai thứ đừng sửa lung tung

- **`Directory.Build.props`** giữ `TargetFramework` cho cả 18 project và
  **`Directory.Packages.props`** giữ version của mọi package. Không khai báo
  `TargetFramework` hay `Version=` riêng trong từng `.csproj`.
- **`global.json`** ghim feature band của SDK; hai workflow CI đọc thẳng file này.
- Package riêng của project test khai báo ở **`src/Tests/Directory.Build.props`**,
  không lặp lại trong từng `.csproj` test.
- **`TreatWarningsAsErrors` đang bật.** Không tắt nó để "cho build qua" — sửa cảnh báo.

### Tạo migration

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

## 9. Bí mật

- **Không commit** `.env`, `appsettings.Development.json` với API key
- API key để trong biến môi trường hoặc user secrets: `dotnet user-secrets set "OpenAI:ApiKey" "sk-..."`
- Nếu lỡ commit key: **rotate key ngay**, đừng chỉ revert commit

## 10. Cần giúp

- Blocker > 4 giờ → tag cả nhóm
- Câu hỏi kiến trúc → TV1
- Câu hỏi AI → TV4
- Câu hỏi API/nghiệp vụ → TV2
- Câu hỏi UI → TV3
