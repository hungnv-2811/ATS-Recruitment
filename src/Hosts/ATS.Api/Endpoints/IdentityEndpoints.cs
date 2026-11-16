using System.Security.Claims;
using ATS.Api.Auth;
using ATS.Api.Http;
using ATS.Identity.Application.Contracts;

namespace ATS.Api.Endpoints;

/// <summary>/api/auth/*, /api/me, /api/admin/users — TV2 so huu.</summary>
/// <remarks>
/// Endpoint chi goi Application service (IAuthService, IUserAdminService) — khong cham DbContext
/// (ArchitectureTests quy tac 3). Route/DTO dung docs/contracts.md muc 8.3.
/// </remarks>
public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var auth = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();
        auth.MapPost("/register", RegisterAsync);
        auth.MapPost("/login", LoginAsync);
        auth.MapPost("/forgot-password", ForgotPasswordAsync);
        auth.MapPost("/reset-password", ResetPasswordAsync);

        app.MapGet("/api/me", GetMeAsync).WithTags("Auth").RequireAuthorization();

        var admin = app.MapGroup("/api/admin/users").WithTags("Admin").RequireAuthorization(AuthPolicies.AdminOnly);
        admin.MapGet("/", ListUsersAsync);
        admin.MapPatch("/{id:guid}", UpdateUserAsync);

        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, IAuthService auth, CancellationToken ct)
        => (await auth.RegisterAsync(request, ct)).Match(r => Results.Ok(r));

    private static async Task<IResult> LoginAsync(LoginRequest request, IAuthService auth, CancellationToken ct)
        => (await auth.LoginAsync(request, ct)).Match(r => Results.Ok(r));

    /// <summary>Luon 204 — khong tiet lo email co ton tai (UC-06).</summary>
    private static async Task<IResult> ForgotPasswordAsync(ForgotPasswordRequest request, IAuthService auth, CancellationToken ct)
        => (await auth.ForgotPasswordAsync(request, ct)).ToNoContent();

    private static async Task<IResult> ResetPasswordAsync(ResetPasswordRequest request, IAuthService auth, CancellationToken ct)
        => (await auth.ResetPasswordAsync(request, ct)).ToNoContent();

    private static async Task<IResult> GetMeAsync(ClaimsPrincipal user, IAuthService auth, CancellationToken ct)
        => (await auth.GetMeAsync(user.GetUserId(), ct)).Match(r => Results.Ok(r));

    private static async Task<IResult> ListUsersAsync(IUserAdminService admin, CancellationToken ct)
        => (await admin.ListUsersAsync(ct)).Match(r => Results.Ok(r));

    private static async Task<IResult> UpdateUserAsync(Guid id, UpdateUserRequest request, ClaimsPrincipal user, IUserAdminService admin, CancellationToken ct)
        => (await admin.UpdateUserAsync(user.GetUserId(), id, request, ct)).ToNoContent();
}
