using System.Security.Cryptography;
using ATS.Web.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace ATS.Web.Services;

/// <summary>Nguoi dung dang dang nhap trong mot circuit Blazor.</summary>
public sealed record UserSession(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    string FullName,
    string Role)
{
    public bool IsCandidate => Role == Roles.Candidate;

    public bool IsHr => Role == Roles.Hr;

    public bool IsAdmin => Role == Roles.Admin;
}

public static class Roles
{
    public const string Candidate = "Candidate";
    public const string Hr = "HR";
    public const string Admin = "Admin";
}

/// <summary>
/// Phien dang nhap cua MOT tab trinh duyet (scoped theo circuit).
/// </summary>
/// <remarks>
/// JWT luu trong <see cref="ProtectedLocalStorage"/> (ma hoa bang Data Protection cua server)
/// de F5 khong mat dang nhap. Web KHONG tu kiem chu ky JWT — API moi la noi ep quyen (RB8);
/// phia Web chi dung vai trong token de an/hien man hinh va chan route (wireframes muc 0.3).
/// </remarks>
public sealed class AuthSession
{
    private const string StorageKey = "ats.session";

    private readonly ProtectedLocalStorage _storage;
    private readonly ILogger<AuthSession> _logger;
    private Task? _loading;

    public AuthSession(ProtectedLocalStorage storage, ILogger<AuthSession> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public event Action? Changed;

    public UserSession? Current { get; private set; }

    public bool IsLoaded { get; private set; }

    public bool IsInRole(string role) => Current?.Role == role;

    /// <summary>Doc phien tu localStorage dung mot lan cho moi circuit.</summary>
    public Task EnsureLoadedAsync() => _loading ??= LoadAsync();

    public async Task SignInAsync(AuthResponse auth)
    {
        ArgumentNullException.ThrowIfNull(auth);

        Current = new UserSession(auth.AccessToken, auth.ExpiresAt, auth.UserId, auth.Email, auth.FullName, auth.Role);
        IsLoaded = true;
        _loading = Task.CompletedTask;

        try
        {
            await _storage.SetAsync(StorageKey, Current);
        }
        catch (InvalidOperationException ex)
        {
            // Chua co JS interop (prerender) — phien van dung trong circuit nay, chi khong nho qua F5.
            _logger.LogWarning(ex, "Khong luu duoc phien dang nhap vao trinh duyet.");
        }

        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        Current = null;
        IsLoaded = true;
        _loading = Task.CompletedTask;

        try
        {
            await _storage.DeleteAsync(StorageKey);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Khong xoa duoc phien dang nhap trong trinh duyet.");
        }

        Changed?.Invoke();
    }

    private async Task LoadAsync()
    {
        try
        {
            var stored = await _storage.GetAsync<UserSession>(StorageKey);
            Current = stored.Success && stored.Value is { } value && value.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1)
                ? value
                : null;
        }
        catch (Exception ex) when (ex is CryptographicException or InvalidOperationException or System.Text.Json.JsonException)
        {
            // Khoa Data Protection doi (container khoi dong lai) hoac du lieu hong: coi nhu chua dang nhap.
            _logger.LogInformation(ex, "Bo qua phien dang nhap cu khong doc duoc.");
            Current = null;
        }

        IsLoaded = true;
        Changed?.Invoke();
    }
}
