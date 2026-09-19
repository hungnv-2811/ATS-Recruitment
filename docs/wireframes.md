# Wireframe

> TV3 phụ trách. Chốt tuần 1, cập nhật khi API đổi.

Tài liệu này là bản phác màn hình **ở mức bố cục và hành vi**, không phải bản thiết kế đồ hoạ.
Mục đích: để TV2 biết API cần trả gì, và để TV3 không phải chờ API xong mới bắt đầu dựng.

Nguyên tắc: **mỗi màn hình ghi rõ nó thuộc tuần nào.** UI bám theo API ngay trong tuần API
xong, không dồn sang tuần 9 — xem [`weekly-plan.md`](weekly-plan.md), mục "Hai điều chỉnh so
với bản nháp đầu".

---

## 0. Bốn quy tắc áp cho mọi màn hình

Bốn thứ này không phải chi tiết thẩm mỹ. Làm sai là sai nghiệp vụ.

### 0.1. Điểm AI luôn đi kèm nguồn

Ba tầng fallback cho **ba thang điểm khác nhau**. Không bao giờ hiện con số trần.

```
  ┌─────────────────────────┐   ┌─────────────────────────┐   ┌─────────────────────────┐
  │  82  ● AI               │   │  74  ◐ Ngữ nghĩa        │   │  61  ○ Từ khoá          │
  │      OpenAI, ngữ cảnh   │   │      Embedding          │   │      Độ chính xác thấp  │
  └─────────────────────────┘   └─────────────────────────┘   └─────────────────────────┘
       adapter = OpenAI              adapter = Embedding           adapter = Keyword
```

Nhãn `Từ khoá` luôn kèm dòng "độ chính xác thấp — nên đọc CV trực tiếp". Khi một bảng xếp hạng
trộn nhiều tầng, hiện cảnh báo ở đầu bảng.

### 0.2. Thiếu điểm AI không được chặn thao tác (RB9)

Hồ sơ `Chưa chấm` hoặc `Không chấm được` **vẫn hiện trong danh sách**, vẫn mở được, HR vẫn
chuyển trạng thái được. Không nút nào bị disable vì lý do "AI chưa xong".

### 0.3. Phân quyền ở cả router, không chỉ ở menu

Ẩn mục menu là chưa đủ. Gõ thẳng URL của trang HR bằng tài khoản ứng viên phải ra
`403`, không phải ra trang trắng (RB8).

### 0.4. Trạng thái chờ

| Thao tác | Kiểu | Hiển thị |
|---|---|---|
| Xem độ phù hợp (1 CV) | đồng bộ, ≤ 30s | spinner trong nút + huỷ được |
| Sàng lọc lô (200–300 CV) | bất đồng bộ | progress bar, polling 2s, rời trang vẫn chạy tiếp |
| Sinh câu hỏi phỏng vấn | đồng bộ | spinner trong nút |

---

## 1. Màn hình của Ứng viên

### UV-1 · Đăng nhập / Đăng ký / Quên mật khẩu — **tuần 3**

```
┌──────────────────────────────────────────────┐
│                ATS Recruitment               │
│                                              │
│   ○ Ứng viên        ○ Nhà tuyển dụng         │  ← chọn vai khi ĐĂNG KÝ
│                                              │
│   Email      [______________________]        │
│   Mật khẩu   [______________________]        │
│                                              │
│            [    Đăng nhập    ]               │
│                                              │
│   Quên mật khẩu?        Chưa có tài khoản?   │
└──────────────────────────────────────────────┘
```

Màn "Quên mật khẩu" sau khi bấm gửi **luôn** hiện cùng một thông báo, dù email có tồn tại hay
không: *"Nếu email này đã đăng ký, chúng tôi đã gửi link đặt lại."* Hiện "email không tồn tại"
là biếu không cho kẻ tấn công công cụ dò tài khoản (UC-06).

### UV-2 · Quản lý CV — **tuần 4**

```
┌─────────────────────────────────────────────────────────────────┐
│  CV của tôi                              [ + Tải CV mới ]       │
├─────────────────────────────────────────────────────────────────┤
│  ★  CV Backend Java 2026       PDF · 412 KB · 02/10   [⋯]       │
│     CV Fullscript             DOCX · 233 KB · 28/09   [⋯]       │
│  ⚠  CV thiết kế               PDF · 1.2 MB · 20/09   [⋯]       │
│     └ Không đọc được nội dung — AI sẽ không chấm được CV này    │
└─────────────────────────────────────────────────────────────────┘
        ★ = CV mặc định        [⋯] = Đặt mặc định · Đổi tên · Xoá
```

CV không trích xuất được text **vẫn cho giữ và vẫn cho nộp** — chỉ đánh dấu là AI không chấm
được (UC-01).

### UV-3 · Danh sách tin — **tuần 4**

```
┌──────────────────────────────────────────────────────────────────┐
│ [ tìm theo vị trí, kỹ năng...        ]  Địa điểm ▾  Hình thức ▾   │
├──────────────────────────────────────────────────────────────────┤
│  Backend Developer (.NET)              Hà Nội · Toàn thời gian   │
│  Yêu cầu: C#, PostgreSQL, Docker                     đăng 2 ngày │
│ ─────────────────────────────────────────────────────────────────│
│  Data Engineer                         Đà Nẵng · Toàn thời gian  │
└──────────────────────────────────────────────────────────────────┘
```

### UV-4 · Chi tiết tin + Xem độ phù hợp + Ứng tuyển — **tuần 5, hoàn thiện tuần 7**

Đây là **màn hình bán hàng của cả đồ án**. Ứng viên biết % phù hợp *trước khi* nộp.

```
┌───────────────────────────────────────────────────────────────────┐
│  Backend Developer (.NET)                      Hà Nội · Full-time │
│  ───────────────────────────────────────────────────────────────  │
│  Mô tả công việc ...                                              │
│  Yêu cầu: C#, PostgreSQL, Docker, 2+ năm kinh nghiệm              │
│                                                                   │
│  ┌─ Độ phù hợp của bạn ────────────────────────────────────────┐  │
│  │  Chọn CV:  [ CV Backend Java 2026        ▾ ]                │  │
│  │                                                             │  │
│  │            [  Xem độ phù hợp  ]     còn 17/20 lượt hôm nay  │  │
│  │                                                             │  │
│  │  ── sau khi chấm ──────────────────────────────────────────  │  │
│  │                                                             │  │
│  │      82 / 100     ● AI                                      │  │
│  │                                                             │  │
│  │      Phù hợp tốt. Kinh nghiệm .NET và PostgreSQL khớp      │  │
│  │      yêu cầu; thiếu kinh nghiệm Docker trong production.    │  │
│  │                                                             │  │
│  │      ✓ Điểm mạnh          C#, PostgreSQL, REST API          │  │
│  │      ✗ Còn thiếu          Docker, Kubernetes                │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
│                          [    Ứng tuyển với CV này    ]           │
└───────────────────────────────────────────────────────────────────┘
```

Ba trạng thái phụ phải dựng:

| Tình huống | Màn hình hiện gì |
|---|---|
| Hết 20 lượt/ngày (`429`) | "Bạn đã dùng hết lượt xem hôm nay. Đặt lại lúc 00:00." — **nút Ứng tuyển vẫn bật** |
| Quá 30s, rơi xuống Keyword | Vẫn hiện điểm, nhãn đổi thành `○ Từ khoá`, kèm câu "độ chính xác thấp" |
| CV chưa đọc được text | "Chưa chấm được CV này", gợi ý chọn CV khác |

Xem lại cùng CV + tin đã chấm → trả cache, **không trừ lượt**, hiện "đã xem trước đó".

### UV-5 · Lịch sử ứng tuyển — **tuần 5**

```
┌────────────────────────────────────────────────────────────────┐
│  Hồ sơ đã nộp                                                  │
├────────────────────────────────────────────────────────────────┤
│  Backend Developer (.NET)                                      │
│  CV Backend Java 2026 · nộp 03/10                              │
│  Trạng thái  ●───●───○───○   Sàng lọc                          │
│  Độ phù hợp  82  ● AI                                          │
│ ───────────────────────────────────────────────────────────────│
│  Data Engineer                                                 │
│  CV Fullscript · nộp 01/10                                     │
│  Trạng thái  ●───○───○───○   Đã nộp                            │
│  Độ phù hợp  — chưa chấm                                       │
└────────────────────────────────────────────────────────────────┘
```

Ứng viên **chỉ xem**, không tự chuyển trạng thái được.

---

## 2. Màn hình của HR

### HR-1 · Quản lý tin — **tuần 4**

```
┌─────────────────────────────────────────────────────────────────┐
│  Tin của tôi                            [ + Đăng tin mới ]      │
├─────────────────────────────────────────────────────────────────┤
│  Backend Developer (.NET)     ● Đang mở    38 hồ sơ    [⋯]      │
│  Data Engineer                ● Đang mở    12 hồ sơ    [⋯]      │
│  QA Engineer                  ○ Đã đóng    54 hồ sơ    [⋯]      │
└─────────────────────────────────────────────────────────────────┘
```

### HR-2 · Ứng viên của một tin — **tuần 6, hoàn thiện tuần 7**

Màn quan trọng nhất phía HR.

```
┌──────────────────────────────────────────────────────────────────────┐
│  ← Backend Developer (.NET) · 38 hồ sơ                               │
│                                                                      │
│  [ Chạy sàng lọc AI cho 12 hồ sơ chưa chấm ]      Lọc: Tất cả ▾      │
│                                                                      │
│  ┌ Đang sàng lọc ────────────────────────────────────────────────┐   │
│  │ ████████████████░░░░░░░░  18/26    2 lỗi    [ Xem chi tiết ]  │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ⚠ Bảng này trộn điểm từ nhiều nguồn khác nhau — xem nhãn từng dòng. │
│                                                                      │
│  Điểm          Ứng viên         CV                 Trạng thái        │
│  ─────────────────────────────────────────────────────────────────── │
│   91  ● AI     Nguyễn V. A      CV Backend 2026    Sàng lọc          │
│   82  ● AI     Trần T. B        CV Java            Đã nộp            │
│   74  ◐ Ngữ…   Lê V. C          CV Fullstack       Đã nộp            │
│   61  ○ Từ khoá Phạm T. D       CV Backend         Đã nộp            │
│   —   chưa chấm Hoàng V. E      CV Junior          Đã nộp            │
│   —   lỗi CV    Đỗ T. F         CV thiết kế        Đã nộp            │
└──────────────────────────────────────────────────────────────────────┘
```

Hai dòng cuối là phần dễ làm sai nhất: **hồ sơ chưa chấm và hồ sơ lỗi vẫn phải nằm trong
bảng, vẫn mở được, vẫn chuyển trạng thái được** (RB9). Sắp xếp theo điểm giảm dần, các dòng
không có điểm xuống cuối — tương ứng `ORDER BY score DESC NULLS LAST` trong
[`database-design.md`](database-design.md) mục 4.1.

Rời khỏi trang khi đang sàng lọc rồi quay lại → progress bar đọc lại từ
`GET /screening-jobs/{id}`, không mất tiến độ.

### HR-3 · Chi tiết hồ sơ — **tuần 6**

```
┌──────────────────────────────────────────────────────────────────────┐
│  ← Nguyễn V. A · Backend Developer (.NET)                            │
│  ┌──────────────────────────┬─────────────────────────────────────┐  │
│  │                          │  91 / 100    ● AI                   │  │
│  │      [ CV gốc – PDF ]    │                                     │  │
│  │                          │  Tóm tắt của AI ...                 │  │
│  │      (bản GỐC, chưa      │                                     │  │
│  │       ẩn danh — chỉ HR   │  ✓ C#, PostgreSQL, Docker, CI/CD    │  │
│  │       xem, không gửi     │  ✗ Kubernetes                       │  │
│  │       lên LLM)           │                                     │  │
│  │                          │  Ghi chú riêng của HR:              │  │
│  │                          │  [_______________________________]  │  │
│  └──────────────────────────┴─────────────────────────────────────┘  │
│                                                                      │
│  Chuyển trạng thái:  [ Sàng lọc ▾ ]   [ Hẹn phỏng vấn ]  [ Từ chối ] │
└──────────────────────────────────────────────────────────────────────┘
```

HR xem **CV gốc** (có tên, SĐT) — điều đó đúng và cần thiết. Thứ bị ẩn danh chỉ là bản gửi
lên LLM (RB3). Ghi rõ điều này trên màn hình để không ai hiểu nhầm.

### HR-4 · Hẹn phỏng vấn + AI gợi ý câu hỏi — **tuần 8**

```
┌──────────────────────────────────────────────────────────────────────┐
│  Hẹn phỏng vấn · Nguyễn V. A                                         │
│                                                                      │
│  Thời gian  [ 12/10/2026  14:00 ]   Hình thức  ○ Online  ○ Tại VP    │
│  Link / địa điểm  [________________]  Người PV  [______________]     │
│                                                                      │
│  ┌ Câu hỏi phỏng vấn ────────────────────────────────────────────┐   │
│  │                              [ ✨ Gợi ý câu hỏi từ CV + JD ]   │   │
│  │                                                               │   │
│  │  Chuyên môn                                                   │   │
│  │   1. [Bạn đã xử lý N+1 query trong EF Core thế nào?     ] [✕] │   │
│  │   2. [Mô tả cách bạn thiết kế index cho bảng lớn        ] [✕] │   │
│  │  Hành vi                                                      │   │
│  │   3. [Kể về lần bạn bất đồng với team lead về kỹ thuật  ] [✕] │   │
│  │                                            [ + Thêm câu hỏi ] │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│                            [ Lưu và gửi lời mời ]                    │
└──────────────────────────────────────────────────────────────────────┘
```

Mọi câu hỏi AI sinh ra đều **sửa và xoá được** — AI gợi ý, HR quyết định. Bỏ qua nút gợi ý và
tự nhập tay cũng phải làm được.

Sau khi lưu: nếu gửi mail thất bại thì hiện dải cảnh báo *"Đã lưu lịch phỏng vấn nhưng chưa
gửi được email"* kèm nút **Gửi lại lời mời**. Tuyệt đối không rollback buổi phỏng vấn (UC-04).

### HR-5 · Nhập đánh giá — **tuần 8**

```
┌────────────────────────────────────────────────────────┐
│  Đánh giá sau phỏng vấn · Nguyễn V. A                  │
│                                                        │
│  Mức độ        ★ ★ ★ ★ ☆                               │
│  Nhận xét      [____________________________________]  │
│  Đề xuất       ○ Nhận   ○ Cân nhắc   ○ Từ chối         │
│                                                        │
│                             [ Lưu đánh giá ]           │
└────────────────────────────────────────────────────────┘
```

Đề xuất của HR **không** tự chuyển trạng thái ứng tuyển. HR vẫn phải bấm chuyển ở màn HR-3 —
hai việc tách nhau để không ai vô tình loại ứng viên chỉ bằng một cú chọn radio.

### HR-6 · Dashboard — **tuần 9 (cắt được)**

```
┌───────────────────────────────────────────────────────────────┐
│  Hồ sơ theo trạng thái        Chi phí AI tháng này            │
│  ▇▇▇▇▇▇▇▇▇▇ Đã nộp    112     $3.42 / $25                     │
│  ▇▇▇▇▇      Sàng lọc   48     ████░░░░░░░░░░░░                │
│  ▇▇         Phỏng vấn  19                                     │
│  ▇          Nhận        6     Tỉ lệ trúng cache: 63%          │
│                                                               │
│  Tin nhiều hồ sơ nhất         Phân bố nguồn điểm              │
│  1. Backend Developer   38    ● AI 71% · ◐ Ngữ nghĩa 18%      │
│  2. QA Engineer         54    ○ Từ khoá 11%                   │
└───────────────────────────────────────────────────────────────┘
```

Hai ô bên phải phục vụ RB2: nhìn một cái là biết còn bao nhiêu ngân sách và cache có đang
làm việc không.

---

## 3. Bản đồ màn hình → tuần → use case

| Màn | Tên | Tuần | Use case | Cắt được? |
|---|---|---|---|---|
| UV-1 | Đăng nhập / đăng ký / quên mật khẩu | 3 | UC-06 | Không |
| UV-2 | Quản lý CV | 4 | UC-01 | Không |
| UV-3 | Danh sách tin | 4 | — | Không |
| UV-4 | Chi tiết tin + độ phù hợp + ứng tuyển | 5 → 7 | UC-02, UC-05 | Không |
| UV-5 | Lịch sử ứng tuyển | 5 | UC-02 | Không |
| HR-1 | Quản lý tin | 4 | — | Không |
| HR-2 | Ứng viên của tin + sàng lọc lô | 6 → 7 | UC-03 | Không |
| HR-3 | Chi tiết hồ sơ | 6 | UC-03 | Không |
| HR-4 | Hẹn phỏng vấn + gợi ý câu hỏi | 8 | UC-04 | Không |
| HR-5 | Nhập đánh giá | 8 | UC-04 | Không |
| HR-6 | Dashboard | 9 | — | **Có** (vị trí 1 trong danh sách cắt) |

Nếu chậm tiến độ, thứ tự cắt bám theo [`weekly-plan.md`](weekly-plan.md) mục "Fallback nếu
chậm tiến độ": bỏ HR-6 trước, sau đó hạ toàn bộ UI xuống form thô + Swagger. **Không cắt**
nhãn nguồn điểm ở mục 0.1 và quy tắc RB9 ở mục 0.2 — hai thứ đó là nghiệp vụ, không phải
trang trí.

---

## 4. Ghi chú kỹ thuật cho TV3

- Component dùng lại được đặt trong `Components/Shared/`: `ScoreBadge` (điểm + nhãn nguồn),
  `StatusPill`, `ScreeningProgress`, `EmptyState`.
- `ScoreBadge` nhận `int? score` và `string? adapterUsed`. `null` → hiện "chưa chấm". Viết
  một lần, dùng ở UV-4, UV-5, HR-2, HR-3 — bốn màn, một nguồn sự thật.
- Không gọi `HttpClient` thẳng trong `.razor`; đi qua service class trong `Services/`
  (xem [`code-review.md`](code-review.md) mục 8).
- Không hardcode URL API — đọc `ApiBaseUrl` từ config, đã wire sẵn ở `Program.cs` tuần 2.
