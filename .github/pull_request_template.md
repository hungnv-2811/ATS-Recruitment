## Việc này làm gì

<!-- 2–3 câu. Vì sao cần làm, không chỉ là làm cái gì. -->

Closes #

## Tầng bị ảnh hưởng

- [ ] `ATS.Contracts` — ⚠️ **cần 2 approve** và **đã báo cả nhóm trước khi mở PR**
- [ ] `ATS.Data`
- [ ] `ATS.Business` / `ATS.Api`
- [ ] `ATS.Web`
- [ ] `ATS.AI` / `ATS.Tests`
- [ ] `docker/` / `.github/`
- [ ] Chỉ tài liệu (`docs/`, `*.md`) — được bỏ qua yêu cầu test

## Cách kiểm thử

<!-- Người review cần làm gì để tự kiểm chứng. Ghi lệnh cụ thể, đừng ghi "chạy thử là thấy". -->

1.
2.

## Ảnh chụp màn hình

<!-- Bắt buộc nếu có thay đổi giao diện. Kéo thả ảnh vào đây. -->

---

## Checklist của tác giả

- [ ] `dotnet build` và `dotnet test` chạy xanh ở máy tôi
- [ ] Đã viết unit test cho phần mới (hoặc PR này chỉ sửa tài liệu)
- [ ] PR dưới ~400 dòng thay đổi — nếu vượt, đã giải thích lý do ở dưới
- [ ] Không commit API key, connection string thật, hay dữ liệu ứng viên thật
- [ ] Không sửa file thuộc tầng người khác (nếu có: đã thống nhất với chủ tầng, ghi tên vào đây)
- [ ] Đã cập nhật tài liệu liên quan trong `docs/` nếu hành vi hệ thống thay đổi
- [ ] Đã tự đọc lại toàn bộ diff của chính mình trước khi gán reviewer

<!-- Nếu PR vượt 400 dòng, giải thích tại sao không tách nhỏ được: -->

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
