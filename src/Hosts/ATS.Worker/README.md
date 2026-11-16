# ATS.Worker

Background service tiêu thụ Redis queue. Process riêng để không ngốn CPU/RAM của API khi chạy sàng lọc lô (ADR-2).

## Composition Root

`Program.cs` đăng ký **giống API** ở mọi chỗ chạm dữ liệu/AI:

- `AddAtsPersistence(...)` với cùng 3 assembly module (Recruitment, AiScreening, Identity) → cùng model `AtsDbContext`.
- `AddSharedInfrastructure` → `AddRecruitmentModule` (cần `IRecruitmentQueries`) → `AddAiScreeningModule` (queue, handler, adapter AI).
- Cùng khoá cấu hình `AiProvider`, `OpenAI:*`, `Ai:*` — trong compose lấy từ `x-app-common` nên không thể lệch API.

## Vòng lặp (`ScreeningWorker`)

```
while (!stopping) {
    using scope = CreateScope();                       // mỗi việc một scope (DbContext riêng)
    queue/handler = scope.GetService<...>();           // resolve LƯỜI — null => log "chua cau hinh", chờ
    task = await queue.DequeueAsync(ct);
    if (task is null) { await Task.Delay(IdleDelay); continue; }
    try { await handler.HandleAsync(task, ct); }
    catch (Exception) { log lỗi, làm việc tiếp }       // một việc lỗi không giết worker
}
```

`Worker:IdleDelay` (mặc định `00:00:05`) là khoảng chờ khi hàng đợi rỗng hoặc vừa gặp lỗi hạ tầng.
