using System.Security.Claims;

namespace ATS.Api.Auth;

/// <summary>
/// Quy uoc JWT chung cho ca nhom — contract tuan 3.
/// </summary>
/// <remarks>
/// JwtTokenIssuer (Identity.Infrastructure) PHAI phat dung cac claim nay:
/// <c>sub</c> = user id (Guid), <c>email</c>, <c>name</c> = ho ten, <c>role</c> = mot trong
/// <see cref="Roles"/>. Program.cs tat MapInboundClaims nen ten claim giu nguyen nhu tren.
/// </remarks>
public static class Roles
{
    public const string Candidate = "Candidate";
    public const string Hr = "HR";
    public const string Admin = "Admin";
}

public static class AuthPolicies
{
    public const string CandidateOnly = "CandidateOnly";
    public const string HrOnly = "HrOnly";
    public const string AdminOnly = "AdminOnly";
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id cua user dang goi (claim <c>sub</c>). Chi goi tren endpoint da RequireAuthorization.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var raw = user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id)
            ? id
            : throw new InvalidOperationException("Token khong co claim 'sub' hop le.");
    }

    public static string? GetRole(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.FindFirstValue("role");
    }
}
