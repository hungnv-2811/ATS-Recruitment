namespace ATS.Persistence.Reporting;

/// <summary>Truy van dashboard (tuan 9). Doc thuan, JOIN xuyen 3 schema.</summary>
public interface IDashboardReportService
{
    /// <param name="hrOwnerUserId">
    /// Id HR (identity.users) — chi tinh tin co <c>jobs.hr_owner_id</c> nay.
    /// <c>null</c> = toan he thong (Admin).
    /// </param>
    /// <param name="ct">Token huy.</param>
    Task<DashboardDto> GetDashboardAsync(Guid? hrOwnerUserId, CancellationToken ct = default);
}
