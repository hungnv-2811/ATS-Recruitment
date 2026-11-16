using ATS.SharedKernel;

namespace ATS.Identity.Domain;

/// <summary>Tai khoan dang nhap — chung cho Ung vien, HR, Admin (bang identity.users).</summary>
/// <remarks>
/// Chi giu thu phuc vu xac thuc. Ho so nghiep vu (Candidate, HrProfile) thuoc module Recruitment.
/// <c>FullName</c> nam o day de JWT phat claim <c>name</c> ma khong phai hoi module khac.
/// </remarks>
public sealed class User : Entity<Guid>
{
    private User()
    {
    }

    private User(Guid id, string email, string passwordHash, Role role, string fullName, DateTimeOffset createdAt)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        FullName = fullName;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public Role Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public static User Create(string email, string passwordHash, Role role, string fullName, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        return new User(Guid.CreateVersion7(), NormalizeEmail(email), passwordHash, role, fullName.Trim(), now);
    }

    /// <summary>Email so sanh khong phan biet hoa thuong: luu mot dang duy nhat.</summary>
    public static string NormalizeEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Trim().ToLowerInvariant();
    }

    public void ChangePassword(string newPasswordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        PasswordHash = newPasswordHash;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    public void ChangeRole(Role role) => Role = role;
}
