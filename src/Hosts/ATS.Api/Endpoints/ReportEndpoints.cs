using ATS.Api.Auth;
using ATS.Persistence.Reporting;

namespace ATS.Api.Endpoints;

/// <summary>/api/reports/* (dashboard tuan 9) — TV1 so huu.</summary>
public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/reports")
            .WithTags("Reports")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Hr, Roles.Admin));

        group.MapGet("/dashboard", GetDashboardAsync)
            .WithName("GetDashboard")
            .Produces<DashboardDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    /// <summary>HR: chi tin cua minh. Admin: toan he thong (contracts.md 8.3).</summary>
    private static async Task<IResult> GetDashboardAsync(
        HttpContext http,
        IDashboardReportService reports,
        CancellationToken ct)
    {
        var user = http.User;
        Guid? scope = user.GetRole() == Roles.Admin ? null : user.GetUserId();

        var dashboard = await reports.GetDashboardAsync(scope, ct).ConfigureAwait(false);
        return Results.Ok(dashboard);
    }
}
