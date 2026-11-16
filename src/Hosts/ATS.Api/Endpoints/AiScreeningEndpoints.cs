namespace ATS.Api.Endpoints;

/// <summary>/api/ai/*, /api/me/ai-quota, /api/screening-jobs, cau hoi phong van — TV4 so huu.</summary>
public static class AiScreeningEndpoints
{
    public static IEndpointRouteBuilder MapAiScreeningEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app;
    }
}
