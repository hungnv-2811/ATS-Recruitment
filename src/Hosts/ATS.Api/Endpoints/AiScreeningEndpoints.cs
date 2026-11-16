using System.Security.Claims;
using ATS.AiScreening.Application.Contracts;
using ATS.AiScreening.Application.Handlers;
using ATS.Api.Auth;
using ATS.Api.Http;
using ATS.SharedKernel;

namespace ATS.Api.Endpoints;

/// <summary>/api/ai/*, /api/me/ai-quota, /api/screening-jobs, cau hoi phong van — TV4 so huu.</summary>
/// <remarks>
/// Dung bang "AI — TV4" o docs/contracts.md muc 8.3. Endpoint chi doc claim + goi handler cua
/// AiScreening.Application — khong cham DbContext (ArchitectureTests quy tac 3).
/// Moi response co diem deu mang <c>adapterUsed</c> (diem Keyword va diem OpenAI khac thang).
/// </remarks>
public static class AiScreeningEndpoints
{
    public static IEndpointRouteBuilder MapAiScreeningEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/ai/score-preview", ScorePreviewAsync)
            .RequireAuthorization(AuthPolicies.CandidateOnly)
            .WithTags("AI")
            .WithName("AiScorePreview");

        app.MapGet("/api/me/ai-quota", GetQuotaAsync)
            .RequireAuthorization()
            .WithTags("AI")
            .WithName("AiQuota");

        app.MapPost("/api/screening-jobs", StartScreeningAsync)
            .RequireAuthorization(AuthPolicies.HrOnly)
            .WithTags("AI")
            .WithName("StartScreeningJob");

        app.MapGet("/api/screening-jobs/{id:guid}", GetScreeningJobAsync)
            .RequireAuthorization(AuthPolicies.HrOnly)
            .WithTags("AI")
            .WithName("GetScreeningJob");

        app.MapGet("/api/jobs/{jobId:guid}/screening-jobs/latest", GetLatestScreeningJobAsync)
            .RequireAuthorization(AuthPolicies.HrOnly)
            .WithTags("AI")
            .WithName("GetLatestScreeningJob");

        app.MapGet("/api/applications/{id:guid}/ai-score", GetAiScoreAsync)
            .RequireAuthorization()
            .WithTags("AI")
            .WithName("GetApplicationAiScore");

        app.MapPost("/api/applications/{id:guid}/questions", GenerateQuestionsAsync)
            .RequireAuthorization(AuthPolicies.HrOnly)
            .WithTags("AI")
            .WithName("GenerateInterviewQuestions");

        app.MapPut("/api/interviews/{id:guid}/questions", SaveQuestionsAsync)
            .RequireAuthorization(AuthPolicies.HrOnly)
            .WithTags("AI")
            .WithName("SaveInterviewQuestions");

        app.MapGet("/api/interviews/{id:guid}/questions", ListQuestionsAsync)
            .RequireAuthorization(AuthPolicies.HrOnly)
            .WithTags("AI")
            .WithName("ListInterviewQuestions");

        return app;
    }

    private static async Task<IResult> ScorePreviewAsync(
        ScorePreviewRequest request,
        ClaimsPrincipal user,
        PreviewScoreHandler handler,
        AiScreeningQueries queries,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await handler.HandleAsync(userId, request, ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return result.Error.Code == Error.QuotaExceeded(string.Empty).Code
            ? await QuotaProblemAsync(result.Error, userId, queries, ct).ConfigureAwait(false)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GetQuotaAsync(ClaimsPrincipal user, AiScreeningQueries queries, CancellationToken ct)
        => Results.Ok(await queries.GetQuotaAsync(user.GetUserId(), ct).ConfigureAwait(false));

    private static async Task<IResult> StartScreeningAsync(
        StartScreeningRequest request,
        ClaimsPrincipal user,
        StartScreeningHandler handler,
        AiScreeningQueries queries,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await handler.HandleAsync(userId, request, ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return Results.Accepted($"/api/screening-jobs/{result.Value.Id}", result.Value);
        }

        return result.Error.Code == Error.QuotaExceeded(string.Empty).Code
            ? await QuotaProblemAsync(result.Error, userId, queries, ct).ConfigureAwait(false)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GetScreeningJobAsync(Guid id, ClaimsPrincipal user, AiScreeningQueries queries, CancellationToken ct)
        => (await queries.GetScreeningJobAsync(user.GetUserId(), id, ct).ConfigureAwait(false)).Match(Results.Ok);

    private static async Task<IResult> GetLatestScreeningJobAsync(Guid jobId, ClaimsPrincipal user, AiScreeningQueries queries, CancellationToken ct)
        => (await queries.GetLatestScreeningJobAsync(user.GetUserId(), jobId, ct).ConfigureAwait(false)).Match(Results.Ok);

    private static async Task<IResult> GetAiScoreAsync(Guid id, ClaimsPrincipal user, AiScreeningQueries queries, CancellationToken ct)
        => (await queries.GetAiScoreAsync(user.GetUserId(), user.GetRole(), id, ct).ConfigureAwait(false)).Match(Results.Ok);

    private static async Task<IResult> GenerateQuestionsAsync(Guid id, ClaimsPrincipal user, GenerateQuestionsHandler handler, CancellationToken ct)
        => (await handler.HandleAsync(user.GetUserId(), id, ct).ConfigureAwait(false)).Match(Results.Ok);

    private static async Task<IResult> SaveQuestionsAsync(
        Guid id,
        SaveQuestionsRequest request,
        ClaimsPrincipal user,
        InterviewQuestionsHandler handler,
        CancellationToken ct)
        => (await handler.SaveAsync(user.GetUserId(), id, request, ct).ConfigureAwait(false)).ToNoContent();

    private static async Task<IResult> ListQuestionsAsync(Guid id, ClaimsPrincipal user, InterviewQuestionsHandler handler, CancellationToken ct)
        => (await handler.ListAsync(user.GetUserId(), id, ct).ConfigureAwait(false)).Match(Results.Ok);

    /// <summary>
    /// 429 theo contracts.md muc 7: ProblemDetails + <c>code</c> + <c>previewRemainingToday</c> + <c>resetsAt</c>,
    /// de UI hien "het luot, mo lai luc ..." ma khong phai goi them /api/me/ai-quota.
    /// </summary>
    private static async Task<IResult> QuotaProblemAsync(Error error, Guid userId, AiScreeningQueries queries, CancellationToken ct)
    {
        var quota = await queries.GetQuotaAsync(userId, ct).ConfigureAwait(false);
        return Results.Problem(
            detail: error.Message,
            statusCode: StatusCodes.Status429TooManyRequests,
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["code"] = error.Code,
                ["previewRemainingToday"] = quota.PreviewRemainingToday,
                ["resetsAt"] = quota.ResetsAt,
            });
    }
}
