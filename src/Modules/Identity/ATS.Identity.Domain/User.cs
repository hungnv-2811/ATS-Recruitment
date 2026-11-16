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
        SecurityStamp = NewStamp();
    }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public Role Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    /// <summary>
    /// Chuoi ngau nhien nam trong JWT (claim <c>sstamp</c>). Doi stamp = vo hieu MOI token da phat.
    /// </summary>
    /// <remarks>
    /// JWT tu chua, API khong tra database khi kiem chu ky — nen khoa tai khoan khong tu lam token
    /// cu het hieu luc. API so stamp trong token voi stamp hien tai moi request; cac thao tac
    /// nhay cam (khoa, doi vai, doi mat khau) deu xoay stamp.
    /// </remarks>
    public string SecurityStamp { get; private set; } = string.Empty;

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
        SecurityStamp = NewStamp();
    }

    public void SetActive(bool isActive)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        SecurityStamp = NewStamp();
    }

    public void ChangeRole(Role role)
    {
        if (Role == role)
        {
            return;
        }

        // Token cu mang vai cu trong claim "role" — phai vo hieu ngay, khong cho dung quyen cu.
        Role = role;
        SecurityStamp = NewStamp();
    }

    private static string NewStamp() => Guid.NewGuid().ToString("N");
}
