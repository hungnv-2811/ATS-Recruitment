using ATS.Identity.Domain;
using ATS.Identity.Domain.Ports;
using ATS.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATS.Identity.Infrastructure.Persistence;

internal sealed class UserRepository : IUserRepository
{
    private readonly AtsDbContext _db;

    public UserRepository(AtsDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = User.NormalizeEmail(email);
        return _db.Set<User>().FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Set<User>().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
        => await _db.Set<User>().AsNoTracking().OrderBy(u => u.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _db.Set<User>().AddAsync(user, ct);
}

internal sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AtsDbContext _db;

    public PasswordResetTokenRepository(AtsDbContext db) => _db = db;

    public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => _db.Set<PasswordResetToken>().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
        => await _db.Set<PasswordResetToken>().AddAsync(token, ct);

    public async Task InvalidateAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default)
    {
        // Nap vao change tracker (khong dung ExecuteUpdate) de nam CUNG SaveChanges voi viec
        // doi mat khau — mot transaction — va chay duoc ca tren provider InMemory khi test.
        var open = await _db.Set<PasswordResetToken>()
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ToListAsync(ct);

        foreach (var token in open)
        {
            token.MarkUsed(now);
        }
    }
}
