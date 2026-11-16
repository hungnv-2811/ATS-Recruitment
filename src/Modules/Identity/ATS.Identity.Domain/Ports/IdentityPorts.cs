namespace ATS.Identity.Domain.Ports;

/// <summary>Kho tai khoan. Entity tra ve duoc EF theo doi: sua xong goi IUnitOfWork.SaveChangesAsync.</summary>
public interface IUserRepository
{
    /// <param name="email">Khong phan biet hoa thuong.</param>
    /// <param name="ct">Token huy.</param>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string plainPassword);

    bool Verify(string plainPassword, string hash);
}

public interface ITokenIssuer
{
    AccessToken IssueAccessToken(User user);
}

/// <summary>JWT da ky cung thoi diem het han (UI can <c>ExpiresAt</c> de tu dang xuat).</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>"Quen mat khau" — token luu dang HASH, dung mot lan, het han 30 phut.</summary>
public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    Task AddAsync(PasswordResetToken token, CancellationToken ct = default);

    /// <summary>Danh dau da dung moi token CON HIEU LUC cua user (sau khi doi mat khau).</summary>
    Task InvalidateAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default);
}
