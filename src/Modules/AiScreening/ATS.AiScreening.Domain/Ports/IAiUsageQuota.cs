namespace ATS.AiScreening.Domain.Ports;

/// <summary>
/// Han muc goi AI theo (user, ngay) — hien thuc RB2 (ngan sach $20–30 ca ky).
/// Trung cache thi KHONG tru luot: khong ton tien thi khong tinh.
/// </summary>
public interface IAiUsageQuota
{
    Task<bool> TryConsumePreviewAsync(Guid userId, CancellationToken ct = default);

    Task<bool> TryConsumeBatchAsync(Guid userId, CancellationToken ct = default);

    Task<QuotaStatus> GetStatusAsync(Guid userId, CancellationToken ct = default);
}
