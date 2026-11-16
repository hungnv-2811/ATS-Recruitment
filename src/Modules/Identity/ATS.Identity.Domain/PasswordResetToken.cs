using ATS.SharedKernel;

namespace ATS.Identity.Domain;

/// <summary>Token "quen mat khau" (bang identity.password_reset_tokens).</summary>
/// <remarks>
/// Chi luu HASH cua token (ro database van khong dat lai duoc mat khau). Het han sau
/// <see cref="Lifetime"/>, dung mot lan (<see cref="UsedAt"/>). Xem UC-06.
/// </remarks>
public sealed class PasswordResetToken : Entity<Guid>
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private PasswordResetToken()
    {
    }

    private PasswordResetToken(Guid id, Guid userId, string tokenHash, DateTimeOffset createdAt)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = createdAt + Lifetime;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static PasswordResetToken Create(Guid userId, string tokenHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new PasswordResetToken(Guid.CreateVersion7(), userId, tokenHash, now);
    }

    /// <summary>Con dung duoc: chua dung va chua het han.</summary>
    public bool IsUsable(DateTimeOffset now) => UsedAt is null && now < ExpiresAt;

    public void MarkUsed(DateTimeOffset now) => UsedAt ??= now;
}
