namespace ATS.Api.Endpoints;

/// <summary>/api/auth/*, /api/me — TV2 so huu.</summary>
public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app;
    }
}
