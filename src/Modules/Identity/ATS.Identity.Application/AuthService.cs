using System.Buffers.Text;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using ATS.Identity.Application.Contracts;
using ATS.Identity.Application.Ports;
using ATS.Identity.Domain;
using ATS.Identity.Domain.Ports;
using ATS.SharedKernel;
using ATS.SharedKernel.Ports;
using Microsoft.Extensions.Logging;

namespace ATS.Identity.Application;

/// <summary>Dang ky / dang nhap / quen mat khau — UC-06 va tuan 3 cua weekly-plan.</summary>
public sealed class AuthService : IAuthService
{
    /// <summary>Thong bao CHUNG cho moi loi token (khong noi ro het han hay da dung — UC-06).</summary>
    public const string InvalidResetTokenMessage = "Link dat lai mat khau khong hop le hoac da het han.";

    private const string InvalidCredentialsMessage = "Email hoac mat khau khong dung.";

    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenRepository _resetTokens;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IUserProfileProvisioner _profiles;
    private readonly IEmailSender _email;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _clock;
    private readonly AuthSettings _settings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IPasswordResetTokenRepository resetTokens,
        IPasswordHasher hasher,
        ITokenIssuer tokenIssuer,
        IUserProfileProvisioner profiles,
        IEmailSender email,
        IUnitOfWork uow,
        TimeProvider clock,
        AuthSettings settings,
        ILogger<AuthService> logger)
    {
        _users = users;
        _resetTokens = resetTokens;
        _hasher = hasher;
        _tokenIssuer = tokenIssuer;
        _profiles = profiles;
        _email = email;
        _uow = uow;
        _clock = clock;
        _settings = settings;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsValidEmail(request.Email))
        {
            return Result.Failure<AuthResponse>(Error.Validation("Email khong hop le."));
        }

        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 200)
        {
            return Result.Failure<AuthResponse>(Error.Validation("Ho ten bat buoc, toi da 200 ky tu."));
        }

        var passwordError = ValidatePassword(request.Password);
        if (passwordError is not null)
        {
            return Result.Failure<AuthResponse>(passwordError);
        }

        // Tu dang ky chi duoc lam Candidate hoac HR; Admin chi sinh tu seed (Seed:AdminEmail).
        if (!RoleNames.TryParse(request.Role, out var role) || role == Role.Admin)
        {
            return Result.Failure<AuthResponse>(Error.Validation("Vai phai la 'Candidate' hoac 'HR'."));
        }

        if (await _users.GetByEmailAsync(request.Email, ct) is not null)
        {
            return Result.Failure<AuthResponse>(Error.Conflict("Email nay da duoc dang ky."));
        }

        var user = User.Create(request.Email, _hasher.Hash(request.Password), role, request.FullName, _clock.GetUtcNow());
        await _users.AddAsync(user, ct);
        await _profiles.EnsureProfileAsync(user.Id, role, user.FullName, ct);

        // Mot SaveChanges = mot transaction: user va ho so cung thanh cong hoac cung khong.
        await _uow.SaveChangesAsync(ct);

        return ToAuthResponse(user);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized(InvalidCredentialsMessage));
        }

        var user = await _users.GetByEmailAsync(request.Email, ct);
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized(InvalidCredentialsMessage));
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized("Tai khoan da bi khoa. Lien he quan tri vien."));
        }

        return ToAuthResponse(user);
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Moi nhanh deu tra Success: tra loi khac nhau cho email la/email that la bieu khong cong cu
        // do tai khoan cho ke tan cong.
        if (!IsValidEmail(request.Email))
        {
            return Result.Success();
        }

        var user = await _users.GetByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive)
        {
            return Result.Success();
        }

        var rawToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        await _resetTokens.AddAsync(PasswordResetToken.Create(user.Id, HashToken(rawToken), _clock.GetUtcNow()), ct);
        await _uow.SaveChangesAsync(ct);

        var link = $"{_settings.WebBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var message = new EmailMessage(
            user.Email,
            "Dat lai mat khau ATS Recruitment",
            $"<p>Xin chao {WebUtility.HtmlEncode(user.FullName)},</p>" +
            $"<p>Bam vao link sau de dat lai mat khau (het han sau {(int)PasswordResetToken.Lifetime.TotalMinutes} phut, dung mot lan):</p>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">{WebUtility.HtmlEncode(link)}</a></p>" +
            "<p>Neu ban khong yeu cau, hay bo qua email nay.</p>",
            $"Dat lai mat khau: {link}");

        try
        {
            await _email.SendAsync(message, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Loi SMTP khong duoc lo ra ngoai (UC-06): ghi log, nguoi dung thu lai sau.
            _logger.LogWarning(ex, "Khong gui duoc email dat lai mat khau cho user {UserId}.", user.Id);
        }

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var passwordError = ValidatePassword(request.NewPassword);
        if (passwordError is not null)
        {
            return Result.Failure(passwordError);
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result.Failure(Error.Validation(InvalidResetTokenMessage));
        }

        var now = _clock.GetUtcNow();
        var token = await _resetTokens.GetByHashAsync(HashToken(request.Token.Trim()), ct);
        if (token is null || !token.IsUsable(now))
        {
            return Result.Failure(Error.Validation(InvalidResetTokenMessage));
        }

        var user = await _users.GetByIdAsync(token.UserId, ct);
        if (user is null || !user.IsActive)
        {
            return Result.Failure(Error.Validation(InvalidResetTokenMessage));
        }

        user.ChangePassword(_hasher.Hash(request.NewPassword));
        token.MarkUsed(now);
        await _resetTokens.InvalidateAllForUserAsync(user.Id, now, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<MeDto>> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        return user is null
            ? Result.Failure<MeDto>(Error.NotFound("Khong tim thay tai khoan."))
            : new MeDto(user.Id, user.Email, user.FullName, RoleNames.ToName(user.Role));
    }

    /// <summary>SHA-256 dang hex chu thuong — gia tri luu o cot token_hash.</summary>
    public static string HashToken(string rawToken)
    {
        ArgumentNullException.ThrowIfNull(rawToken);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }

    internal static Error? ValidatePassword(string? password)
        => string.IsNullOrEmpty(password) || password.Length < AuthSettings.MinPasswordLength
            ? Error.Validation($"Mat khau toi thieu {AuthSettings.MinPasswordLength} ky tu.")
            : null;

    private static bool IsValidEmail(string? email)
        => !string.IsNullOrWhiteSpace(email)
           && email.Trim().Length <= 256
           && MailAddress.TryCreate(email.Trim(), out var parsed)
           && string.Equals(parsed.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);

    private AuthResponse ToAuthResponse(User user)
    {
        var token = _tokenIssuer.IssueAccessToken(user);
        return new AuthResponse(token.Value, token.ExpiresAt, user.Id, user.Email, user.FullName, RoleNames.ToName(user.Role));
    }
}
