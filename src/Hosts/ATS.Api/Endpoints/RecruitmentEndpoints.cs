using System.Security.Claims;
using ATS.Api.Auth;
using ATS.Api.Http;
using ATS.Recruitment.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ATS.Api.Endpoints;

/// <summary>/api/jobs, /api/hr/*, /api/me/cvs, /api/me/applications, /api/applications, /api/interviews — TV2 so huu.</summary>
/// <remarks>
/// Route/DTO dung docs/contracts.md muc 8.3. Policy chi chan theo VAI; quyen CHU SO HUU (HR chu
/// tin, ung vien chu CV/don) do Application service kiem — RB8 ep o server, khong o UI.
/// Trang thai/enum trong DTO da la chuoi nen khong can cau hinh JSON rieng.
/// </remarks>
public static class RecruitmentEndpoints
{
    public static IEndpointRouteBuilder MapRecruitmentEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // ---- Tin tuyen dung --------------------------------------------------------------
        var jobs = app.MapGroup("/api/jobs").WithTags("Jobs");
        jobs.MapGet("/", SearchJobsAsync).AllowAnonymous();
        jobs.MapGet("/{id:guid}", GetJobAsync).AllowAnonymous();
        jobs.MapPost("/", CreateJobAsync).RequireAuthorization(AuthPolicies.HrOnly);
        jobs.MapPatch("/{id:guid}", UpdateJobAsync).RequireAuthorization(AuthPolicies.HrOnly);
        jobs.MapPost("/{id:guid}/publish", PublishJobAsync).RequireAuthorization(AuthPolicies.HrOnly);
        jobs.MapPost("/{id:guid}/close", CloseJobAsync).RequireAuthorization(AuthPolicies.HrOnly);
        jobs.MapGet("/{jobId:guid}/applications", ListJobApplicationsAsync)
            .WithTags("Applications")
            .RequireAuthorization(AuthPolicies.HrOnly);

        var hr = app.MapGroup("/api/hr").RequireAuthorization(AuthPolicies.HrOnly);
        hr.MapGet("/jobs", ListMyJobsAsync).WithTags("Jobs");
        hr.MapGet("/interviews", ListMyInterviewsAsync).WithTags("Interviews");

        // ---- CV cua ung vien ------------------------------------------------------------
        var cvs = app.MapGroup("/api/me/cvs").WithTags("CVs").RequireAuthorization(AuthPolicies.CandidateOnly);
        cvs.MapGet("/", ListCvsAsync);
        cvs.MapPost("/", UploadCvAsync).DisableAntiforgery();
        cvs.MapDelete("/{id:guid}", DeleteCvAsync);
        cvs.MapPatch("/{id:guid}/default", SetDefaultCvAsync);

        app.MapGet("/api/me/applications", ListMyApplicationsAsync)
            .WithTags("Applications")
            .RequireAuthorization(AuthPolicies.CandidateOnly);

        // ---- Don ung tuyen + phong van --------------------------------------------------
        var applications = app.MapGroup("/api/applications").WithTags("Applications");
        applications.MapPost("/", ApplyAsync).RequireAuthorization(AuthPolicies.CandidateOnly);
        applications.MapGet("/{id:guid}", GetApplicationAsync).RequireAuthorization();
        applications.MapGet("/{id:guid}/cv-file", GetApplicationCvFileAsync).RequireAuthorization();
        applications.MapPatch("/{id:guid}/status", ChangeStatusAsync).RequireAuthorization(AuthPolicies.HrOnly);
        applications.MapPost("/{id:guid}/interviews", ScheduleInterviewAsync)
            .WithTags("Interviews")
            .RequireAuthorization(AuthPolicies.HrOnly);
        applications.MapGet("/{id:guid}/interviews", ListApplicationInterviewsAsync)
            .WithTags("Interviews")
            .RequireAuthorization();

        var interviews = app.MapGroup("/api/interviews").WithTags("Interviews").RequireAuthorization(AuthPolicies.HrOnly);
        interviews.MapPost("/{id:guid}/resend-invite", ResendInviteAsync);
        interviews.MapPost("/{id:guid}/evaluations", SubmitEvaluationAsync);

        return app;
    }

    // ---- Jobs ---------------------------------------------------------------------------

    private static async Task<IResult> SearchJobsAsync(
        IJobService jobs,
        CancellationToken ct,
        string? search = null,
        string? location = null,
        string? employmentType = null,
        int page = 1,
        int pageSize = 20)
        => (await jobs.SearchAsync(new JobSearchQuery(search, location, employmentType, page, pageSize), ct))
            .Match(r => Results.Ok(r));

    /// <summary>Endpoint an danh: co token hop le thi biet nguoi goi de cho HR chu tin xem tin Draft/Closed.</summary>
    private static async Task<IResult> GetJobAsync(Guid id, ClaimsPrincipal user, IJobService jobs, CancellationToken ct)
    {
        Guid? caller = Guid.TryParse(user.FindFirstValue("sub"), out var userId) ? userId : null;
        return (await jobs.GetAsync(id, caller, ct)).Match(r => Results.Ok(r));
    }

    private static async Task<IResult> ListMyJobsAsync(ClaimsPrincipal user, IJobService jobs, CancellationToken ct)
        => (await jobs.ListMineAsync(user.GetUserId(), ct)).Match(r => Results.Ok(r));

    private static async Task<IResult> CreateJobAsync(CreateJobRequest request, ClaimsPrincipal user, IJobService jobs, CancellationToken ct)
        => (await jobs.CreateAsync(user.GetUserId(), request, ct)).Match(r => Results.Created($"/api/jobs/{r.Id}", r));

    private static async Task<IResult> UpdateJobAsync(Guid id, UpdateJobRequest request, ClaimsPrincipal user, IJobService jobs, CancellationToken ct)
        => (await jobs.UpdateAsync(user.GetUserId(), id, request, ct)).Match(r => Results.Ok(r));

    private static async Task<IResult> PublishJobAsync(Guid id, ClaimsPrincipal user, IJobService jobs, CancellationToken ct)
        => (await jobs.PublishAsync(user.GetUserId(), id, ct)).ToNoContent();

    private static async Task<IResult> CloseJobAsync(Guid id, ClaimsPrincipal user, IJobService jobs, CancellationToken ct)
        => (await jobs.CloseAsync(user.GetUserId(), id, ct)).ToNoContent();

    // ---- CVs ----------------------------------------------------------------------------

    private static async Task<IResult> ListCvsAsync(ClaimsPrincipal user, ICvService cvs, CancellationToken ct)
        => (await cvs.ListAsync(user.GetUserId(), ct)).Match(r => Results.Ok(r));

    /// <summary>multipart/form-data: <c>file</c> (pdf/docx, toi da 5 MB) + <c>name</c>.</summary>
    private static async Task<IResult> UploadCvAsync(
        IFormFile file,
        [FromForm] string name,
        ClaimsPrincipal user,
        ICvService cvs,
        CancellationToken ct)
    {
        await using var content = file.OpenReadStream();
        var result = await cvs.UploadAsync(
            user.GetUserId(),
            new UploadCvCommand(name, file.FileName, file.Length, content),
            ct);
        return result.Match(r => Results.Created($"/api/me/cvs/{r.Id}", r));
    }

    private static async Task<IResult> DeleteCvAsync(Guid id, ClaimsPrincipal user, ICvService cvs, CancellationToken ct)
        => (await cvs.DeleteAsync(user.GetUserId(), id, ct)).ToNoContent();

    private static async Task<IResult> SetDefaultCvAsync(Guid id, ClaimsPrincipal user, ICvService cvs, CancellationToken ct)
        => (await cvs.SetDefaultAsync(user.GetUserId(), id, ct)).ToNoContent();

    // ---- Applications -------------------------------------------------------------------

    private static async Task<IResult> ApplyAsync(ApplyRequest request, ClaimsPrincipal user, IApplicationService applications, CancellationToken ct)
        => (await applications.ApplyAsync(user.GetUserId(), request, ct))
            .Match(r => Results.Created($"/api/applications/{r.Id}", r));

    private static async Task<IResult> ListMyApplicationsAsync(ClaimsPrincipal user, IApplicationService applications, CancellationToken ct)
        => (await applications.ListMineAsync(user.GetUserId(), ct)).Match(r => Results.Ok(r));

    private static async Task<IResult> ListJobApplicationsAsync(
        Guid jobId,
        ClaimsPrincipal user,
        IApplicationService applications,
        CancellationToken ct,
        string? status = null)
        => (await applications.ListForJobAsync(user.GetUserId(), jobId, status, ct)).Match(r => Results.Ok(r));

    private static async Task<IResult> GetApplicationAsync(Guid id, ClaimsPrincipal user, IApplicationService applications, CancellationToken ct)
        => (await applications.GetDetailAsync(user.GetUserId(), id, ct)).Match(r => Results.Ok(r));

    private static async Task<IResult> GetApplicationCvFileAsync(Guid id, ClaimsPrincipal user, IApplicationService applications, CancellationToken ct)
        => (await applications.GetCvFileAsync(user.GetUserId(), id, ct))
            .Match(f => Results.File(f.Content, f.ContentType, f.FileName));

    private static async Task<IResult> ChangeStatusAsync(Guid id, ChangeStatusRequest request, ClaimsPrincipal user, IApplicationService applications, CancellationToken ct)
        => (await applications.ChangeStatusAsync(user.GetUserId(), id, request, ct)).ToNoContent();

    // ---- Interviews ---------------------------------------------------------------------

    private static async Task<IResult> ScheduleInterviewAsync(Guid id, ScheduleInterviewRequest request, ClaimsPrincipal user, IInterviewService interviews, CancellationToken ct)
        => (await interviews.ScheduleAsync(user.GetUserId(), id, request, ct))
            .Match(r => Results.Created($"/api/applications/{id}/interviews", r));

    private static async Task<IResult> ListApplicationInterviewsAsync(Guid id, ClaimsPrincipal user, IInterviewService interviews, CancellationToken ct)
        => (await interviews.ListByApplicationAsync(user.GetUserId(), id, ct)).Match(r => Results.Ok(r));

    private static async Task<IResult> ListMyInterviewsAsync(ClaimsPrincipal user, IInterviewService interviews, CancellationToken ct)
        => (await interviews.ListUpcomingForHrAsync(user.GetUserId(), ct)).Match(r => Results.Ok(r));

    private static async Task<IResult> ResendInviteAsync(Guid id, ClaimsPrincipal user, IInterviewService interviews, CancellationToken ct)
        => (await interviews.ResendInviteAsync(user.GetUserId(), id, ct)).ToNoContent();

    private static async Task<IResult> SubmitEvaluationAsync(Guid id, SubmitEvaluationRequest request, ClaimsPrincipal user, IInterviewService interviews, CancellationToken ct)
        => (await interviews.SubmitEvaluationAsync(user.GetUserId(), id, request, ct))
            .Match(r => Results.Created($"/api/interviews/{id}/evaluations", r));
}
