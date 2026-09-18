# Quy ước làm việc

## 1. Nhánh

| Nhánh | Vai trò |
|---|---|
| `main` | Ổn định, đã deploy. Không commit trực tiếp. |
| `develop` | Tích hợp. Không commit trực tiếp. |
| `feature/*` | Từng việc. Tạo từ `develop`, merge lại vào `develop`. |

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

- PR từ `feature/*` → `develop`. PR từ `develop` → `main` chỉ khi có release.
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
├── Shared/ATS.SharedKernel/
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
3. **Controllers không được inject `DbContext`, phải qua Application service.**

## 7. Chạy local

```bash
# 1. Clone
git clone https://github.com/hungnv-2811/ATS-Recruitment.git
cd ATS-Recruitment

# 2. Copy env mẫu
cp .env.example .env
# Sửa .env: điền OPENAI_API_KEY nếu có (không bắt buộc, Fake adapter chạy được)

# 3. Chạy
docker compose -f docker/docker-compose.yml up -d

# 4. Kiểm tra
curl http://localhost:5000/health
```

## 8. Trước khi push

```bash
dotnet build          # phải xanh
dotnet test           # phải xanh (bao gồm ArchitectureTests)
dotnet format         # định dạng code
```

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
