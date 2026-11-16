using System.Text;
using ATS.Identity.Application.Contracts;
using ATS.Identity.Domain;
using ATS.Identity.Domain.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ATS.Identity.Application;

namespace ATS.Identity.Infrastructure.Security;

/// <summary>BCrypt (BCrypt.Net-Next) — salt nam san trong chuoi hash.</summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private readonly int _workFactor;

    /// <param name="workFactor">11 cho production; test co the ha xuong 4 cho nhanh.</param>
    public BCryptPasswordHasher(int workFactor = 11) => _workFactor = workFactor;

    public string Hash(string plainPassword) => BCrypt.Net.BCrypt.HashPassword(plainPassword, _workFactor);

    public bool Verify(string plainPassword, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash hong trong DB = khong dang nhap duoc, khong phai 500.
            return false;
        }
    }
}

/// <summary>
/// Phat JWT HS256 voi dung bo claim chot o contracts.md 8.1: <c>sub</c>, <c>email</c>,
/// <c>name</c>, <c>role</c> ("Candidate" | "HR" | "Admin"), them <c>sstamp</c> de thu hoi token.
/// </summary>
/// <remarks>
/// Doc <c>Jwt:Secret/Issuer/Audience/ExpiresMinutes</c> tu IConfiguration — Program.cs ghi nguoc
/// gia tri da chon vao configuration nen ben phat va ben kiem luon dung cung mot khoa.
/// </remarks>
public sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly SigningCredentials _credentials;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _lifetime;
    private readonly TimeProvider _clock;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenIssuer(IConfiguration configuration, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var secret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException("Jwt:Secret phai co it nhat 32 byte (HS256).");
        }

        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);
        _issuer = configuration["Jwt:Issuer"] ?? "ats-recruitment";
        _audience = configuration["Jwt:Audience"] ?? "ats-recruitment-users";
        _lifetime = TimeSpan.FromMinutes(configuration.GetValue("Jwt:ExpiresMinutes", 120));
        _clock = clock;
    }

    public AccessToken IssueAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = _clock.GetUtcNow();
        var expires = now + _lifetime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _issuer,
            Audience = _audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
                [JwtRegisteredClaimNames.Name] = user.FullName,
                ["role"] = RoleNames.ToName(user.Role),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
                [TokenClaims.SecurityStamp] = user.SecurityStamp,
            },
        };

        return new AccessToken(_handler.CreateToken(descriptor), expires);
    }

    /// <summary>Doc lai claim tu token (dung trong test).</summary>
    public static IReadOnlyDictionary<string, string> ReadClaims(string token)
        => new JsonWebToken(token).Claims
            .GroupBy(c => c.Type, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
}
