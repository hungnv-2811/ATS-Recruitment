# ATS.Worker

Background service tiêu thụ Redis queue.

## Vòng lặp

```
while (!ct.IsCancellationRequested) {
    var task = await _queue.DequeueAsync(ct);
    if (task is null) continue;
    await _processHandler.HandleAsync(task, ct);
}
```

Process riêng để không ngốn CPU/RAM của API khi chạy sàng lọc lô.
