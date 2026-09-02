# Hướng dẫn code review

> Bổ trợ cho [CONTRIBUTING.md](../CONTRIBUTING.md) mục 3. Tài liệu này trả lời câu hỏi:
> **người review phải nhìn cái gì?**

Nhóm chia việc theo tầng, nên người review thường **không quen tay** với tầng của người mở PR.
Nếu không có tiêu chí cụ thể, buổi review sẽ trôi thành "nhìn qua thấy ổn → approve", và cả nhóm
mất cả chất lượng lẫn 15% điểm tiêu chí *Git, PR, review*. Tài liệu này cho mỗi tầng một danh
sách ngắn những thứ **đáng tìm nhất ở tầng đó**.

---

## 1. Ba việc làm trước khi đọc dòng code nào

1. **Đọc issue được liên kết.** Không biết PR định giải quyết gì thì không thể biết nó giải
   quyết đúng hay chưa. PR không có `Closes #N` → yêu cầu bổ sung, chưa review.
2. **Xem CI đã xanh chưa.** CI đỏ thì trả lại ngay, đừng review. Người review không phải là
   trình biên dịch.
3. **Nhìn số dòng thay đổi.** Vượt ~400 dòng mà không có lý do chính đáng → yêu cầu tách PR.
   Việc này khó nói nhưng phải nói: PR to là chỗ lỗi trốn được.

---

## 2. Ba câu hỏi áp cho mọi PR

| Câu hỏi | Vì sao quan trọng |
|---|---|
| **Code này có làm đúng thứ issue yêu cầu không?** | Lỗi phổ biến nhất không phải code sai, mà là code đúng cho một bài toán khác |
| **Có làm hỏng ranh giới kiến trúc không?** | Xem mục 3 — đây là thứ đắt nhất để sửa về sau |
| **Sáu tháng nữa đọc lại có hiểu không?** | Tên biến, tên hàm, và những chỗ *cần comment mà không có* |

---

## 3. Ranh giới kiến trúc — luôn phải kiểm

Đây là phần **quan trọng nhất** của review trong dự án này, vì `ATS.Tests` chỉ bắt được một phần,
phần còn lại phải do mắt người bắt.

- [ ] Tầng trên gọi tầng dưới, **không có chiều ngược lại**. `ATS.Data` không được biết gì về
      `ATS.Business`; `ATS.Business` không được biết gì về `ATS.Api`.
- [ ] Module này **không gọi thẳng vào lớp `internal` của module khác** — phải đi qua interface
      công khai trong `ATS.Contracts`.
- [ ] **Không có kiểu dữ liệu của hạ tầng rò rỉ lên tầng nghiệp vụ**: không thấy `DbContext`,
      `IQueryable`, `HttpContext`, `IFormFile`, hay kiểu của SDK nhà cung cấp AI trong
      `ATS.Business`.
- [ ] **Entity của EF Core không bị trả thẳng ra ngoài API** — phải map sang DTO trong
      `ATS.Contracts` (nếu không, đổi lược đồ CSDL là vỡ hợp đồng API).
- [ ] Thay đổi `ATS.Contracts` đã được báo cả nhóm và có **2 approve** chưa.

---

## 4. Danh sách kiểm theo từng tầng

### 4.1. `ATS.Data` — dữ liệu và hạ tầng (PR của TV1, TV2 review)

- [ ] Migration có **tương ứng đúng** với thay đổi entity không — và có **chạy ngược được**
      (`Down`) không?
- [ ] Quan hệ và ràng buộc: khoá ngoại, `required`, độ dài chuỗi, hành vi khi xoá
      (`OnDelete`) có đúng nghiệp vụ không? Xoá một tin tuyển dụng có làm bay theo cả lượt
      ứng tuyển của ứng viên không?
- [ ] Có **index** cho cột thường xuyên lọc/sắp xếp (trạng thái ứng tuyển, ngày tạo, khoá ngoại)?
- [ ] Truy vấn có dính **N+1** không: vòng lặp bên trong có gọi lại CSDL không, có thiếu
      `Include` không?
- [ ] Truy vấn chỉ đọc có `AsNoTracking()` không?
- [ ] Có ai đó vô tình `.ToList()` sớm rồi mới `.Where()` trên bộ nhớ không?

### 4.2. `ATS.Business` / `ATS.Api` — nghiệp vụ và API (PR của TV2, TV3 review)

- [ ] **Quy tắc nghiệp vụ nằm trong `ATS.Business`, không nằm trong controller.** Controller
      phải mỏng: nhận, gọi service, trả kết quả.
- [ ] Chuyển trạng thái tuyển dụng có đi đúng state machine không — có đường tắt nào cho phép
      nhảy từ *Mới nộp* thẳng sang *Đã tuyển* không?
- [ ] **Phân quyền**: endpoint mới đã gắn `[Authorize]` với đúng vai trò chưa? HR của bộ phận
      này có xem được ứng viên của bộ phận khác không?
- [ ] Dữ liệu vào đã được **validate** chưa, và lỗi trả về có mã HTTP đúng không
      (400 vs 404 vs 409)?
- [ ] Thông báo lỗi trả cho client có **rò rỉ chi tiết nội bộ** không (stack trace, tên bảng,
      chuỗi kết nối)?
- [ ] Thao tác ghi nhiều bảng có nằm trong **một transaction** không?

### 4.3. `ATS.Web` — giao diện (PR của TV3, TV2 review)

- [ ] Có xử lý đủ **ba trạng thái**: đang tải / rỗng / lỗi? Hay chỉ vẽ được trường hợp đẹp?
- [ ] Khi API trả lỗi, người dùng **thấy gì**? Màn hình trắng là không đạt.
- [ ] Có **quy tắc nghiệp vụ bị nhét vào giao diện** không (tính điểm, quyết định trạng thái)?
      Giao diện chỉ hiển thị và thu thập.
- [ ] Nút gửi có bị **bấm hai lần tạo hai bản ghi** không?
- [ ] Với màn hình sàng lọc: có hiển thị rõ **trạng thái đang chấm / chưa chấm / không chấm
      được** không (xem `architecture.md` ADR-02)? Có lối để HR **mở CV gốc** không?

### 4.4. `ATS.AI` — trợ lý sàng lọc (PR của TV4, TV1 review)

- [ ] **Bước ẩn danh có chắc chắn chạy trước mọi lời gọi ra ngoài không?** Đây là mục kiểm
      nghiêm trọng nhất trong toàn bộ tài liệu này — rò rỉ CCCD/SĐT của ứng viên ra API bên
      ngoài vừa vi phạm dữ liệu cá nhân, vừa rơi đúng một trong 8 tình huống AI **không đạt**
      của học phần.
- [ ] Có **fallback** khi nhà cung cấp lỗi không, và fallback đã được test chưa?
- [ ] Có **cache theo khoá bất biến** (hash CV + hash JD + phiên bản prompt + mã mô hình) không?
      Thiếu cache là đốt tiền thật.
- [ ] Retry có **giới hạn số lần** và có phân biệt lỗi tạm thời với lỗi vĩnh viễn không?
      Retry một CV hỏng 3 lần là trả tiền 3 lần cho cùng một thất bại.
- [ ] Prompt có được **ghi kèm phiên bản** không (để giải thích được vì sao điểm hôm nay khác
      hôm qua)?
- [ ] Test có chạy được **không cần mạng và không cần API key** không (dùng `FakeScoringAdapter`)?

### 4.5. `docker/`, `.github/` — hạ tầng (PR của TV1, TV4 review)

- [ ] Có bí mật nào bị hardcode trong `Dockerfile` / `docker-compose.yml` / workflow không?
- [ ] Ảnh Docker có ghim phiên bản cụ thể không (`postgres:16-alpine`, không dùng `latest`)?
- [ ] Dữ liệu CSDL và file CV có nằm trên **volume** không — hay `docker compose down` là mất sạch?

---

## 5. Không cần review những thứ này

Để dành sức cho thứ đáng đọc:

- **Định dạng code, khoảng trắng, thứ tự `using`.** Đó là việc của `dotnet format` và
  `.editorconfig`, không phải việc của người.
- **Gu đặt tên khi cả hai cách đều ổn.** Nếu muốn nói, dùng `[góp ý]` và đừng chặn merge.
- **Viết lại theo cách mình thích.** Câu hỏi là "code này có đúng và có dễ bảo trì không",
  không phải "mình có viết như thế này không".

---

## 6. Viết comment thế nào

Dùng tiền tố mức độ đã quy ước trong mẫu PR: `[blocking]` · `[nên sửa]` · `[góp ý]` · `[hỏi]`.

**Nói vào code, đừng nói vào người.** So sánh:

> ❌ "Em viết cái này ẩu quá."
>
> ✅ `[blocking]` Chỗ này nếu `cvText` rỗng (PDF ảnh scan không trích được text) thì
> `ScoreAsync` sẽ ném `ArgumentException` và cả lô 300 CV dừng lại. Nên bỏ qua CV đó, ghi log,
> và đánh dấu `SummaryStatus = Unavailable`.

Comment tốt có ba phần: **chuyện gì xảy ra** → **trong tình huống nào** → **đề xuất làm gì**.
Comment như trên còn có tác dụng phụ rất có lợi: đó chính là **minh chứng code review** mà
giảng viên sẽ đọc. Một PR có 3 comment kiểu này giá trị hơn 30 PR chỉ có chữ "LGTM".

**Có gì tốt thì cũng nói.** Review chỉ toàn lỗi sẽ làm người ta ngại mở PR, mà nhóm còn phải
làm chung 10 tuần.

---

## 7. Người mở PR ứng xử thế nào

- **Trả lời từng comment**, kể cả khi chỉ để nói "đã sửa" hoặc "xin giữ nguyên vì…". Push đè
  im lặng khiến người review phải đọc lại từ đầu.
- **Không đồng ý thì phản biện**, đừng sửa cho xong. Người review có thể sai — họ không thạo
  tầng của bạn bằng bạn.
- **Đừng nhận comment là công kích cá nhân.** Review nhắm vào code.
- Sửa xong thì **bấm re-request review**, đừng đợi người kia tự phát hiện.

---

## 8. Kiểm tra minh chứng — cuối mỗi tuần, TV1 làm

Đây là phần chấm điểm, không phải phần kỹ thuật. Cuối tuần, nhóm trưởng soát:

- [ ] Tuần này có **≥ 1 PR đã merge cho mỗi thành viên** không? Ai không có PR nào là dấu hiệu
      tắc dây chuyền hoặc đang commit chui.
- [ ] Có PR nào được approve mà **không có một comment nội dung nào** không? Approve trắng lặp
      lại nhiều lần sẽ bị nhìn ra ngay khi chấm.
- [ ] Reviewer có **phân bố đều** không, hay tất cả PR đều do một người approve?
- [ ] Có commit nào vào thẳng `develop`/`main` mà không qua PR không? GitHub tự thêm `(#N)` vào
      cuối tiêu đề commit khi squash-merge một PR, nên lệnh dưới đây liệt kê những commit **không**
      đến từ PR nào — kết quả lý tưởng là rỗng:

      git log --first-parent develop --format='%s' | grep -v '(#[0-9]\+)$'
- [ ] Mọi PR đã merge đều đóng đúng issue của nó chưa?

Đưa kết quả soát này vào phần *Vướng mắc* của worklog nếu có vấn đề — đừng để dồn tới tuần 10.
