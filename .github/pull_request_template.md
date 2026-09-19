## Việc này làm gì

<!-- 2–3 câu. Vì sao cần làm, không chỉ là làm cái gì. -->

Closes #

## Phần bị ảnh hưởng

- [ ] **Port / contract** (`*.Domain/Ports/`, `SharedKernel/Ports/`) — ⚠️ **cần 2 approve** và
      **đã báo cả nhóm trước khi mở PR**
- [ ] `ATS.SharedKernel` / `ATS.Persistence` (migration → **TV1 review bắt buộc**)
- [ ] `ATS.Recruitment.*` / `ATS.Identity.*` / `ATS.Api`
- [ ] `ATS.Web`
- [ ] `ATS.AiScreening.*` / `src/Tests/` (→ **TV4 review bắt buộc**)
- [ ] `docker/` / `.github/` / `Directory.*.props`
- [ ] Chỉ tài liệu (`docs/`, `*.md`) — được bỏ qua yêu cầu test

### Nếu PR chạm module AI, xác nhận thêm

- [ ] Không thêm `InternalsVisibleTo` vào `ATS.AiScreening.Domain` (phá ADR-3 — xem
      `src/Tests/ATS.ArchitectureTests/README.md`)
- [ ] Port AI vẫn chỉ nhận `AnonymizedCv`, không nhận `string`
- [ ] Mọi chỗ hiển thị điểm AI đều kèm `AdapterUsed`

## Cách kiểm thử

<!-- Người review cần làm gì để tự kiểm chứng. Ghi lệnh cụ thể, đừng ghi "chạy thử là thấy". -->

1.
2.

## Ảnh chụp màn hình

<!-- Bắt buộc nếu có thay đổi giao diện. Kéo thả ảnh vào đây. -->

---

## Checklist của tác giả

- [ ] `dotnet build` xanh và **không có warning** (`TreatWarningsAsErrors` đang bật)
- [ ] `dotnet test` xanh, gồm cả 5 quy tắc `ATS.ArchitectureTests`
- [ ] Nếu sửa `Directory.Packages.props` hoặc Dockerfile: đã thử `docker compose build`
      (build local xanh **không** bảo đảm build Docker xanh)
- [ ] Đã viết unit test cho phần mới (hoặc PR này chỉ sửa tài liệu)
- [ ] Không commit API key, connection string thật, hay dữ liệu ứng viên thật
- [ ] Không sửa file thuộc tầng người khác (nếu có: đã thống nhất với chủ tầng, ghi tên vào đây)
- [ ] Đã cập nhật tài liệu liên quan trong `docs/` nếu hành vi hệ thống thay đổi
- [ ] Đã tự đọc lại toàn bộ diff của chính mình trước khi gán reviewer

## Dành cho người review

Tiêu chí review theo từng tầng: [docs/code-review.md](../docs/code-review.md)

Quy ước mức độ khi để lại comment:

| Tiền tố | Nghĩa | Tác giả phải làm gì |
|---|---|---|
| `[blocking]` | Sai/rủi ro, chưa merge được | **Bắt buộc** sửa hoặc phản biện lại trước khi merge |
| `[nên sửa]` | Không sai nhưng có cách tốt hơn | Sửa, hoặc trả lời vì sao giữ nguyên |
| `[góp ý]` | Gu cá nhân, kiến thức chia sẻ | Tuỳ tác giả, không chặn merge |
| `[hỏi]` | Chưa hiểu chỗ này | Trả lời để người review hiểu |

Approve chỉ khi **không còn `[blocking]` nào chưa xử lý** và CI xanh.
