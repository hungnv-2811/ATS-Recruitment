using ATS.AiScreening.Domain.Entities;

namespace ATS.AiScreening.Domain.Ports;

/// <summary>Bang <c>ai_scores</c>: mot dong / don ung tuyen. Moi method tu luu (khong can UnitOfWork rieng).</summary>
public interface IAiScoreRepository
{
    Task<AiScore?> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>Diem cua nhieu don mot luc — dung cho danh sach xep hang cua Recruitment.</summary>
    Task<IReadOnlyList<AiScore>> ListByApplicationsAsync(IReadOnlyCollection<Guid> applicationIds, CancellationToken ct = default);

    /// <summary>Mot don bat ky da dung ket qua cua dong cache nay (thong ke / truy vet).</summary>
    Task<AiScore?> GetByCacheKeyAsync(string cacheKey, CancellationToken ct = default);

    /// <summary>Them moi neu don chua co diem, nguoc lai cap nhat — khong bao gio sinh dong thu hai.</summary>
    Task UpsertAsync(AiScore score, CancellationToken ct = default);
}
