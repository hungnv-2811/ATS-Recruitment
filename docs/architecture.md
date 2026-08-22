# Kiến trúc hệ thống

> TV1 phụ trách tài liệu này. Cập nhật trong tuần 1–2.

## 1. Sơ đồ nhiều lớp

```
┌──────────────────────────────────────────┐
│  ATS.Web        — Blazor                 │  TV3
├──────────────────────────────────────────┤
│  ATS.Api        — ASP.NET Core Web API   │  TV2
│  ATS.Business   — Service / nghiệp vụ    │  TV2
├──────────────────────────────────────────┤
│  ATS.Data       — EF Core + Repository   │  TV1
├──────────────────────────────────────────┤
│  SQL Server                              │
└──────────────────────────────────────────┘
                    ↕ HTTP
              ┌───────────────┐
              │   ATS.AI      │  TV4
              └───────────────┘

  ATS.Contracts — DTO + interface dùng chung (cả nhóm)
```

**Quy tắc phụ thuộc:** tầng trên chỉ gọi tầng dưới, không có chiều ngược lại.
Mọi tầng đều tham chiếu `ATS.Contracts`.

## 2. Luồng dữ liệu — chức năng sàng lọc CV

```
HR upload CV
   → ATS.Web gửi multipart tới ATS.Api
   → ATS.Business gọi ICvService: lưu file, trích text (PdfPig)
   → ATS.Data lưu metadata CV vào SQL Server
   → ATS.Business gọi IAiScoringService (ATS.AI)
        → ẩn danh dữ liệu nhạy cảm
        → embedding CV + JD → cosine similarity → điểm 0–100
        → LLM sinh tóm tắt + lý do
   → ATS.Data lưu vào bảng AI_Scores
   → ATS.Web hiển thị danh sách ứng viên đã xếp hạng
```

## 3. Cần bổ sung

- [ ] Sơ đồ thành phần (component diagram)
- [ ] Sơ đồ triển khai (deployment diagram) — container nào chạy ở đâu
- [ ] Quyết định kiến trúc: vì sao tách `ATS.AI` thành service riêng thay vì gọi trực tiếp
- [ ] Cơ chế xác thực: JWT phát hành ở đâu, lưu ở đâu, hết hạn bao lâu
