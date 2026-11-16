namespace ATS.Api.Endpoints;

/// <summary>/api/jobs, /api/me/cvs, /api/applications, /api/interviews — TV2 so huu.</summary>
public static class RecruitmentEndpoints
{
    public static IEndpointRouteBuilder MapRecruitmentEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app;
    }
}
