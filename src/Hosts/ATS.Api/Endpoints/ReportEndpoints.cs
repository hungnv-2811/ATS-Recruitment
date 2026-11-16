namespace ATS.Api.Endpoints;

/// <summary>/api/reports/* (dashboard tuan 9) — TV1 so huu.</summary>
public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app;
    }
}
