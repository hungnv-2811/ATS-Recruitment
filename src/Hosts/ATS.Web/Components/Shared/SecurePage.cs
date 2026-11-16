using ATS.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ATS.Web.Components.Shared;

public enum GuardState
{
    Checking,
    Allowed,
    Forbidden,
}

/// <summary>
/// Lop co so cho trang can dang nhap. Chan o ROUTER, khong chi an menu (wireframes muc 0.3, RB8).
/// </summary>
/// <remarks>
/// Chua dang nhap → chuyen ve /login kem returnUrl. Sai vai → hien 403 ngay tai cho thay vi
/// trang trang. Du lieu chi duoc tai trong <see cref="OnAuthorizedAsync"/>, nen trang HR khong
/// bao gio goi API khi nguoi xem la ung vien.
/// API van la lop chan that (policy theo vai); day la lop thu hai cho trai nghiem dung.
/// </remarks>
public abstract class SecurePage : ComponentBase, IDisposable
{
    [Inject]
    protected AuthSession Session { get; set; } = default!;

    [Inject]
    protected NavigationManager Nav { get; set; } = default!;

    protected GuardState Guard { get; private set; } = GuardState.Checking;

    /// <summary>Rong = moi vai da dang nhap deu vao duoc.</summary>
    protected virtual IReadOnlyCollection<string> AllowedRoles => [];

    protected UserSession CurrentUser => Session.Current
        ?? throw new InvalidOperationException("Chi goi CurrentUser sau khi da qua guard.");

    public void Dispose()
    {
        Session.Changed -= OnSessionChanged;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    protected override async Task OnInitializedAsync()
    {
        Session.Changed += OnSessionChanged;
        await Session.EnsureLoadedAsync();

        if (Session.Current is null)
        {
            RedirectToLogin();
            return;
        }

        if (AllowedRoles.Count > 0 && !AllowedRoles.Contains(Session.Current.Role))
        {
            Guard = GuardState.Forbidden;
            return;
        }

        Guard = GuardState.Allowed;
        await OnAuthorizedAsync();
    }

    /// <summary>Tai du lieu cua trang — chi chay khi nguoi xem dung vai.</summary>
    protected virtual Task OnAuthorizedAsync() => Task.CompletedTask;

    private void RedirectToLogin()
    {
        var returnUrl = Nav.ToBaseRelativePath(Nav.Uri);
        Nav.NavigateTo($"login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    private void OnSessionChanged()
    {
        if (Session.Current is null)
        {
            _ = InvokeAsync(RedirectToLogin);
        }
    }
}
