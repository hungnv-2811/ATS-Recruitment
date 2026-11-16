using ATS.SharedKernel;

namespace ATS.AiScreening.Domain.Entities;

/// <summary>
/// Bo dem han muc AI theo (user, ngay UTC) — bang <c>aiscreening.ai_usage_quotas</c>, hien thuc RB2.
/// </summary>
/// <remarks>
/// Luu trong DB chu khong trong bo nho: api va worker la hai process, restart la mat sach.
/// Tren PostgreSQL adapter tang bang mot cau <c>INSERT ... ON CONFLICT DO UPDATE</c> nguyen tu;
/// cac method duoi day la duong di cho provider khac (test InMemory) va la dac ta nghiep vu.
/// </remarks>
public sealed class AiUsageQuota : Entity<Guid>
{
    private AiUsageQuota(Guid id, Guid userId, DateOnly usageDate, DateTimeOffset now)
        : base(id)
    {
        UserId = userId;
        UsageDate = usageDate;
        UpdatedAt = now;
    }

    /// <summary>Danh cho EF Core.</summary>
    private AiUsageQuota()
    {
    }

    public Guid UserId { get; private set; }

    public DateOnly UsageDate { get; private set; }

    public int PreviewCount { get; private set; }

    public int BatchCount { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static AiUsageQuota Create(Guid userId, DateOnly usageDate, DateTimeOffset now)
        => new(Guid.CreateVersion7(), userId, usageDate, now);

    public bool TryConsumePreview(int limitPerDay, DateTimeOffset now)
    {
        if (PreviewCount >= limitPerDay)
        {
            return false;
        }

        PreviewCount++;
        UpdatedAt = now;
        return true;
    }

    public bool TryConsumeBatch(int limitPerDay, DateTimeOffset now)
    {
        if (BatchCount >= limitPerDay)
        {
            return false;
        }

        BatchCount++;
        UpdatedAt = now;
        return true;
    }
}
