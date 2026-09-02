# ATS.AI

**Chủ sở hữu: TV4.**

Project này vừa là **thư viện pipeline**, vừa là **host của tiến trình worker** chạy nền —
**không phải** một web service HTTP (xem `docs/architecture.md` — ADR-02).

```
Pipeline/    trích xuất PDF → ẩn danh PII → rút trường → embedding → cosine → LLM → xếp hạng
Providers/   các adapter của ADR-03: OpenAI, AzureOpenAI, LocalModel, EmbeddingOnly, Fake
Worker/      vòng lặp tiêu thụ hàng đợi Redis: nhận việc, retry, ghi kết quả, dead-letter
```

Chạy khi phát triển (cần `db` và `queue` đang chạy):

```bash
dotnet run --project src/ATS.AI
```

Ba ràng buộc **không được vi phạm**:

1. Cổng `IAiScoringService` nhận kiểu `AnonymizedCv`, **không nhận `string`** — để quên bước ẩn
   danh là lỗi biên dịch chứ không phải lỗi lọt tới lúc review.
2. Mọi kết quả phải ghi kèm `CacheKey`, `ModelVersion`, `PromptVersion`.
3. Test phải chạy được **không cần mạng và không cần API key** (dùng `FakeScoringAdapter`).

> Thư mục còn trống — project .NET sẽ được tạo ở tuần 2.
