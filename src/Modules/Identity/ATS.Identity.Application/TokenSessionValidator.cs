using ATS.Identity.Domain.Ports;

namespace ATS.Identity.Application;

/// <summary>Ten claim rieng cua he thong (ngoai sub/email/name/role chuan).</summary>
public static class TokenClaims
{
    /// <summary>Security stamp cua user luc phat token — xem <see cref="Domain.User.SecurityStamp"/>.</summary>
    public const string SecurityStamp = "sstamp";
}

/// <summary>
/// Kiem token CON hieu luc theo trang thai hien tai cua tai khoan — goi o MOI request da xac thuc.
/// </summary>
/// <remarks>
/// Kiem chu ky + han dung (JwtBearer) chua du: token tu chua nen khong biet tai khoan vua bi
/// khoa, doi vai hay doi mat khau. Composition Root (ATS.Api) goi interface nay trong
/// <c>JwtBearerEvents.OnTokenValidated</c>; test factory co the thay bang ban gia.
/// </remarks>
public interface ITokenSessionValidator
{
    Task<bool> IsValidAsync(Guid userId, string? securityStamp, CancellationToken ct = default);
}

/// <summary>
/// Hop le khi user ton tai, dang hoat dong, va stamp trong token trung stamp hien tai.
/// </summary>
/// <remarks>
/// Mot lan tra theo khoa chinh moi request — re voi quy mo do an. Neu thanh nut co chai, them
/// cache ngan (vai chuc giay) va chap nhan do tre thu hoi tuong ung.
/// </remarks>
public sealed class TokenSessionValidator : ITokenSessionValidator
{
    private readonly IUserRepository _users;

    public TokenSessionValidator(IUserRepository users) => _users = users;

    public async Task<bool> IsValidAsync(Guid userId, string? securityStamp, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(securityStamp))
        {
            // Token phat truoc khi co stamp, hoac token tu che: buoc dang nhap lai.
            return false;
        }

        var user = await _users.GetByIdAsync(userId, ct);
        return user is { IsActive: true }
               && string.Equals(user.SecurityStamp, securityStamp, StringComparison.Ordinal);
    }
}
