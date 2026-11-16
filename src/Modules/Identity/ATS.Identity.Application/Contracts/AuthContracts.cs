using ATS.Identity.Domain;
using ATS.SharedKernel;

namespace ATS.Identity.Application.Contracts;

// DTO dung ten truong o docs/contracts.md muc 8.3 (Auth / Identity). Role luon la CHUOI
// "Candidate" | "HR" | "Admin" — trung gia tri claim "role" trong JWT.

public sealed record RegisterRequest(string Email, string Password, string FullName, string Role);

public sealed record LoginRequest(string Email, string Password);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    string FullName,
    string Role);

public sealed record MeDto(Guid UserId, string Email, string FullName, string Role);

public sealed record UserDto(Guid Id, string Email, string FullName, string Role, bool IsActive, DateTimeOffset CreatedAt);

public sealed record UpdateUserRequest(bool? IsActive, string? Role);

/// <summary>Dang ky, dang nhap, quen/dat lai mat khau, thong tin user dang dang nhap.</summary>
public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>LUON thanh cong — khong tiet lo email co ton tai hay khong (UC-06).</summary>
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);

    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);

    Task<Result<MeDto>> GetMeAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>Quan tri tai khoan — chi Admin (khoa/mo, doi vai).</summary>
public interface IUserAdminService
{
    Task<Result<IReadOnlyList<UserDto>>> ListUsersAsync(CancellationToken ct = default);

    Task<Result> UpdateUserAsync(Guid adminUserId, Guid userId, UpdateUserRequest request, CancellationToken ct = default);
}

/// <summary>Anh xa <see cref="Role"/> &lt;-&gt; chuoi tren JWT/API ("HR" chu khong phai "Hr").</summary>
public static class RoleNames
{
    public const string Candidate = "Candidate";
    public const string Hr = "HR";
    public const string Admin = "Admin";

    public static string ToName(Role role) => role switch
    {
        Role.Candidate => Candidate,
        Role.Hr => Hr,
        Role.Admin => Admin,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };

    /// <summary>Khong phan biet hoa thuong: "hr", "HR", "Hr" deu hop le.</summary>
    public static bool TryParse(string? value, out Role role)
    {
        switch (value?.Trim().ToUpperInvariant())
        {
            case "CANDIDATE":
                role = Role.Candidate;
                return true;
            case "HR":
                role = Role.Hr;
                return true;
            case "ADMIN":
                role = Role.Admin;
                return true;
            default:
                role = default;
                return false;
        }
    }
}
