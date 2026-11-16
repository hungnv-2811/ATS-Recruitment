using ATS.AiScreening.Domain.Entities;

namespace ATS.AiScreening.Domain.Ports;

/// <summary>Bang <c>screening_jobs</c> — tien do sang loc lo.</summary>
public interface IScreeningJobRepository
{
    Task<ScreeningJob?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lo moi nhat cua mot tin — UI noi lai thanh tien do sau khi roi trang.</summary>
    Task<ScreeningJob?> GetLatestByJobAsync(Guid jobId, CancellationToken ct = default);

    Task AddAsync(ScreeningJob job, CancellationToken ct = default);

    /// <summary>
    /// CONG THEM <paramref name="done"/>/<paramref name="failed"/> vao bo dem (khong phai gan gia tri tuyet doi),
    /// tu chuyen Done khi du TargetCount. Tren PostgreSQL la mot cau UPDATE nguyen tu, nen nhieu
    /// worker chay song song khong de mat dem.
    /// </summary>
    Task UpdateProgressAsync(Guid id, int done, int failed, CancellationToken ct = default);
}
