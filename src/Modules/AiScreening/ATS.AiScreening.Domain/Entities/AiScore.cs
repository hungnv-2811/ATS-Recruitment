using ATS.SharedKernel;

namespace ATS.AiScreening.Domain.Entities;

/// <summary>
/// ANH CHUP diem AI cua mot don ung tuyen (bang <c>aiscreening.ai_scores</c>) — dung con so
/// HR da nhin thay. Moi don toi da MOT dong (unique <c>application_id</c>); cham lai thi cap nhat.
/// </summary>
/// <remarks>
/// Sao chep score/summary tu cache thay vi chi tro sang: doi prompt sau nay thi cache cu vo dung,
/// nhung diem HR da can cu de quyet dinh van phai con nguyen (database-design.md muc 2).
/// <see cref="ScreeningStatus"/> doc lap voi trang thai ung tuyen (RB9).
/// </remarks>
public sealed class AiScore : Entity<Guid>
{
    private AiScore(Guid id, Guid applicationId, DateTimeOffset now)
        : base(id)
    {
        ApplicationId = applicationId;
        ScreeningStatus = ScreeningStatus.Pending;
        UpdatedAt = now;
    }

    /// <summary>Danh cho EF Core.</summary>
    private AiScore()
    {
    }

    public Guid ApplicationId { get; private set; }

    /// <summary>0–100; null khi chua cham xong hoac Failed.</summary>
    public int? Score { get; private set; }

    public string? Summary { get; private set; }

    public IReadOnlyList<string> Strengths { get; private set; } = [];

    public IReadOnlyList<string> Gaps { get; private set; } = [];

    public ScreeningStatus ScreeningStatus { get; private set; }

    /// <summary>"OpenAI" | "Embedding" | "Keyword" | "Fake" — mot phan cua ket qua, khong phai cot debug.</summary>
    public string? AdapterUsed { get; private set; }

    public string? ModelVersion { get; private set; }

    /// <summary>Dong cache sinh ra ket qua nay; null khi Keyword (khong ghi cache).</summary>
    public Guid? CacheId { get; private set; }

    /// <summary>Ly do that bai (khong chua noi dung CV) — de HR/dev biet vi sao Failed.</summary>
    public string? FailureReason { get; private set; }

    public bool IsHumanReviewed { get; private set; }

    public DateTimeOffset? ScoredAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static AiScore CreatePending(Guid applicationId, DateTimeOffset now)
    {
        if (applicationId == Guid.Empty)
        {
            throw new ArgumentException("ApplicationId khong duoc rong.", nameof(applicationId));
        }

        return new AiScore(Guid.CreateVersion7(), applicationId, now);
    }

    /// <summary>Xep lai vao hang doi (cham lai). Giu diem cu cho toi khi co diem moi.</summary>
    public void MarkPending(DateTimeOffset now)
    {
        ScreeningStatus = ScreeningStatus.Pending;
        FailureReason = null;
        UpdatedAt = now;
    }

    public void MarkProcessing(DateTimeOffset now)
    {
        ScreeningStatus = ScreeningStatus.Processing;
        UpdatedAt = now;
    }

    public void MarkScored(ScreeningResult result, string modelVersion, Guid? cacheId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(result);

        Score = result.Score.Value;
        Summary = result.Summary;
        Strengths = result.Strengths.ToArray();
        Gaps = result.Gaps.ToArray();
        AdapterUsed = result.AdapterUsed;
        ModelVersion = modelVersion;
        CacheId = cacheId;
        ScreeningStatus = ScreeningStatus.Scored;
        FailureReason = null;
        ScoredAt = now;
        UpdatedAt = now;
    }

    public void MarkFailed(string reason, DateTimeOffset now)
    {
        Score = null;
        Summary = null;
        Strengths = [];
        Gaps = [];
        AdapterUsed = null;
        CacheId = null;
        ScreeningStatus = ScreeningStatus.Failed;
        FailureReason = reason.Length > 500 ? reason[..500] : reason;
        UpdatedAt = now;
    }
}
