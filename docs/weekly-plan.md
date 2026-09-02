# Kế hoạch 10 tuần

Mỗi tuần cả nhóm cùng làm **một nhóm chức năng**, mỗi người làm phần tầng của mình.

| Tuần | Chức năng chung | TV1 — Data & Hạ tầng | TV2 — Nghiệp vụ & API | TV3 — Giao diện | TV4 — AI & Test |
|---|---|---|---|---|---|
| 1 | Khởi động | Tạo repo, cấu trúc solution, board, **bật branch protection + CODEOWNERS** | Phân tích yêu cầu, use case tổng quát | Wireframe màn hình, chọn thư viện UI | Nghiên cứu pipeline AI, đăng ký API key |
| 2 | **Nền tảng + Contract** | ERD, entity, `DbContext`, migration đầu, CI cơ bản, **compose `db` + `queue` để cả nhóm dùng chung** | **Chủ trì chốt `ATS.Contracts`** | Khung Blazor, layout, mock service | **Demo AI cốt lõi** (console) |
| 3 | Tài khoản & phân quyền | Bảng User/Role, seed | AuthService, JWT, policy phân quyền | Trang đăng nhập/đăng ký, lưu token | Unit test auth; trích xuất text PDF; **test ranh giới kiến trúc (NetArchTest) chạy trong CI** |
| 4 | Tin tuyển dụng (Job/JD) | Entity Job, migration, index tìm kiếm | JobService + JobController | Trang danh sách / chi tiết / form Job | Test Job; **bước ẩn danh + kiểu `AnonymizedCv`** |
| 5 | Ứng viên & CV | Entity Candidate/CV, `IFileStorage`, metadata | CvService (upload, trích text), controller | Trang upload CV, danh sách ứng viên | Test upload; embedding + cosine |
| 6 | Quy trình tuyển dụng + **dựng khung bất đồng bộ** | Bảng `ScreeningJobs`, `AI_Scores` (`CacheKey` unique index) | State machine; **endpoint `POST /screening-runs` đẩy việc vào hàng đợi, trả 202** | Màn hình luồng ứng tuyển; **UI trạng thái *đang chấm*** | **Host worker chạy được end-to-end với `FakeScoringAdapter`**; test state machine |
| 7 | **Tích hợp AI thật** | Cache, log, chỉ số chi phí | Đọc kết quả, xếp hạng, hiển thị tiến độ | Màn hình xếp hạng + polling tiến độ + nút phản hồi HR | **Thay Fake bằng adapter OpenAI; retry, dead-letter, fallback 3 cấp; golden set 10–20 cặp CV–JD** |
| 8 | Docker & triển khai | **Compose đủ 5 container + deploy demo** | Hoàn thiện API, chuẩn hoá xử lý lỗi | Hoàn thiện UI, responsive | Test tích hợp trên bản deploy |
| 9 | Báo cáo & ổn định | Truy vấn thống kê, tối ưu index, **service container Postgres/Redis cho CI** | API báo cáo & thống kê | Dashboard biểu đồ, xuất báo cáo | **Test E2E, hồi quy, sửa lỗi** |
| 10 | Hoàn thiện | Tài liệu kiến trúc + CI/CD | Tài liệu API | Slide + kịch bản demo | `ai-integration.md` + báo cáo kiểm thử |

> **Vì sao hạ tầng bất đồng bộ phải xuống tuần 2 và tuần 6, không đợi tuần 8.** Bản kế hoạch
> trước xếp toàn bộ Docker vào tuần 8, trong khi tuần 7 đã phải tích hợp AI chạy nền — tức là
> tuần 7 cần hàng đợi và worker mà tuần 8 mới dựng. Đó là lỗi trình tự, không phải lỗi ước
> lượng công. Cách chữa: **tuần 2** dựng sẵn `db` + `queue` bằng compose để cả nhóm phát triển
> trên hạ tầng thật, **tuần 6** dựng khung worker chạy end-to-end với `FakeScoringAdapter`
> (không tốn tiền, không cần mạng), **tuần 7** chỉ còn việc cắm adapter thật vào. Nhờ vậy tuần 7
> không phải vừa dựng hạ tầng vừa gỡ lỗi prompt cùng lúc.

## Mốc bàn giao

| Mốc | Sản phẩm |
|---|---|
| Cuối tuần 1 | Báo cáo tổng quan + repo + board backlog + **branch protection đã bật** |
| **Cuối tuần 2** | Sơ đồ CSDL + **`ATS.Contracts` đã chốt** + demo AI cốt lõi + `docker compose up db queue` chạy được |
| Cuối tuần 6 | **Luồng sàng lọc chạy thông end-to-end với adapter giả** — bấm nút → 202 → worker xử lý → UI thấy kết quả |
| Cuối tuần 7 | Dịch vụ AI thật, có cache, retry và fallback 3 cấp |
| Cuối tuần 8 | Bản deploy chạy được, đủ 5 container |
| Cuối tuần 10 | Báo cáo cuối + slide + demo |

## Đường lùi nếu trễ tiến độ

Quyết định **trước** khi trễ, để lúc trễ không phải cắt bừa. Cắt theo thứ tự từ trên xuống:

| Thứ tự cắt | Cắt gì | Mất gì | Giữ được gì |
|---|---|---|---|
| 1 | `LocalModelScoringAdapter` và `AzureOpenAiScoringAdapter` | Chỉ còn 1 nhà cung cấp thật | Kiến trúc port/adapter vẫn nguyên vẹn — vẫn giải thích và bảo vệ được |
| 2 | Container `web` riêng | Blazor Server chạy chung tiến trình API | Vẫn đủ Docker, đủ deploy |
| 3 | Test ranh giới rút còn 3–4 test tiêu biểu | Bao phủ hẹp hơn | Cơ chế cưỡng chế vẫn tồn tại và vẫn chạy trong CI |
| 4 | Module *Báo cáo & thống kê* rút còn 2–3 số liệu | Dashboard mỏng | Vẫn đủ 5 nhóm chức năng theo yêu cầu học phần |
| **Không cắt** | Hàng đợi + worker, bước ẩn danh, fallback, PR review, CI, Docker | | Đây là những thứ **trực tiếp ra điểm** hoặc **không thể vá lại ở tuần 10** |
