using System.Globalization;
using System.Net.Http.Headers;
using ATS.Web.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace ATS.Web.Services;

// Moi lop o day tuong ung mot nhom endpoint trong docs/contracts.md muc 8.3.
// Giu MONG: khong logic nghiep vu, chi ghep URL va kieu du lieu.

public sealed class AuthApi(ApiClient api, AuthSession session)
{
    public async Task<ApiResult> LoginAsync(LoginRequest request)
    {
        var result = await api.PostAsync<AuthResponse>("api/auth/login", request);
        if (result is { IsSuccess: true, Value: { } auth })
        {
            await session.SignInAsync(auth);
        }

        return result;
    }

    public async Task<ApiResult> RegisterAsync(RegisterRequest request)
    {
        var result = await api.PostAsync<AuthResponse>("api/auth/register", request);
        if (result is { IsSuccess: true, Value: { } auth })
        {
            await session.SignInAsync(auth);
        }

        return result;
    }

    public Task<ApiResult> ForgotPasswordAsync(string email)
        => api.PostAsync("api/auth/forgot-password", new ForgotPasswordRequest(email));

    public Task<ApiResult> ResetPasswordAsync(string token, string newPassword)
        => api.PostAsync("api/auth/reset-password", new ResetPasswordRequest(token, newPassword));
}

public sealed class CvApi(ApiClient api)
{
    /// <summary>Gioi han 5 MB — trung voi API (contracts.md 8.3) de bao loi som, khoi upload thua.</summary>
    public const long MaxFileBytes = 5 * 1024 * 1024;

    public Task<ApiResult<List<CvDto>>> ListAsync() => api.GetAsync<List<CvDto>>("api/me/cvs");

    public async Task<ApiResult<CvDto>> UploadAsync(IBrowserFile file, string name, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        using var content = new MultipartFormDataContent();
        var stream = new StreamContent(file.OpenReadStream(MaxFileBytes, ct));
        stream.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(stream, "file", file.Name);
        content.Add(new StringContent(name), "name");

        return await api.PostContentAsync<CvDto>("api/me/cvs", content, ct);
    }

    public Task<ApiResult> DeleteAsync(Guid id) => api.DeleteAsync($"api/me/cvs/{id}");

    public Task<ApiResult> SetDefaultAsync(Guid id) => api.PatchAsync($"api/me/cvs/{id}/default", null);
}

public sealed class JobApi(ApiClient api)
{
    public Task<ApiResult<PagedResult<JobDto>>> SearchAsync(
        string? search, string? location, string? employmentType, int page, int pageSize = 10)
    {
        var query = new List<string>
        {
            $"page={page.ToString(CultureInfo.InvariantCulture)}",
            $"pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}",
        };
        AddIfPresent(query, "search", search);
        AddIfPresent(query, "location", location);
        AddIfPresent(query, "employmentType", employmentType);

        return api.GetAsync<PagedResult<JobDto>>($"api/jobs?{string.Join('&', query)}");
    }

    public Task<ApiResult<JobDto>> GetAsync(Guid id) => api.GetAsync<JobDto>($"api/jobs/{id}");

    public Task<ApiResult<List<JobDto>>> ListMineAsync() => api.GetAsync<List<JobDto>>("api/hr/jobs");

    public Task<ApiResult<JobDto>> CreateAsync(CreateJobRequest request) => api.PostAsync<JobDto>("api/jobs", request);

    public Task<ApiResult<JobDto>> UpdateAsync(Guid id, UpdateJobRequest request)
        => api.PatchAsync<JobDto>($"api/jobs/{id}", request);

    public Task<ApiResult> PublishAsync(Guid id) => api.PostAsync($"api/jobs/{id}/publish", null);

    public Task<ApiResult> CloseAsync(Guid id) => api.PostAsync($"api/jobs/{id}/close", null);

    private static void AddIfPresent(List<string> query, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Add($"{key}={Uri.EscapeDataString(value.Trim())}");
        }
    }
}

public sealed class ApplicationApi(ApiClient api)
{
    public Task<ApiResult<ApplicationDto>> ApplyAsync(Guid jobId, Guid cvId)
        => api.PostAsync<ApplicationDto>("api/applications", new ApplyRequest(jobId, cvId));

    public Task<ApiResult<List<ApplicationDto>>> ListMineAsync()
        => api.GetAsync<List<ApplicationDto>>("api/me/applications");

    public Task<ApiResult<List<RankedApplicationDto>>> ListForJobAsync(Guid jobId, string? status)
        => api.GetAsync<List<RankedApplicationDto>>(string.IsNullOrEmpty(status)
            ? $"api/jobs/{jobId}/applications"
            : $"api/jobs/{jobId}/applications?status={Uri.EscapeDataString(status)}");

    public Task<ApiResult<ApplicationDetailDto>> GetAsync(Guid id)
        => api.GetAsync<ApplicationDetailDto>($"api/applications/{id}");

    public Task<ApiResult> ChangeStatusAsync(Guid id, string status, string? note)
        => api.PatchAsync($"api/applications/{id}/status", new ChangeStatusRequest(status, note));

    public Task<ApiResult<DownloadedFile>> DownloadCvAsync(Guid id)
        => api.DownloadAsync($"api/applications/{id}/cv-file");
}

public sealed class InterviewApi(ApiClient api)
{
    public Task<ApiResult<InterviewDto>> ScheduleAsync(Guid applicationId, ScheduleInterviewRequest request)
        => api.PostAsync<InterviewDto>($"api/applications/{applicationId}/interviews", request);

    public Task<ApiResult<List<InterviewDto>>> ListForApplicationAsync(Guid applicationId)
        => api.GetAsync<List<InterviewDto>>($"api/applications/{applicationId}/interviews");

    public Task<ApiResult<List<InterviewListItemDto>>> ListMineAsync()
        => api.GetAsync<List<InterviewListItemDto>>("api/hr/interviews");

    public Task<ApiResult> ResendInviteAsync(Guid interviewId)
        => api.PostAsync($"api/interviews/{interviewId}/resend-invite", null);

    public Task<ApiResult<EvaluationDto>> EvaluateAsync(Guid interviewId, SubmitEvaluationRequest request)
        => api.PostAsync<EvaluationDto>($"api/interviews/{interviewId}/evaluations", request);
}

public sealed class AiApi(ApiClient api)
{
    public Task<ApiResult<ScorePreviewResponse>> PreviewAsync(Guid cvId, Guid jobId, CancellationToken ct)
        => api.PostAsync<ScorePreviewResponse>("api/ai/score-preview", new ScorePreviewRequest(cvId, jobId), ct);

    public Task<ApiResult<QuotaDto>> GetQuotaAsync() => api.GetAsync<QuotaDto>("api/me/ai-quota");

    public Task<ApiResult<ScreeningJobDto>> StartScreeningAsync(Guid jobId)
        => api.PostAsync<ScreeningJobDto>("api/screening-jobs", new StartScreeningRequest(jobId));

    public Task<ApiResult<ScreeningJobDto>> GetScreeningJobAsync(Guid id)
        => api.GetAsync<ScreeningJobDto>($"api/screening-jobs/{id}");

    public Task<ApiResult<ScreeningJobDto>> GetLatestScreeningJobAsync(Guid jobId)
        => api.GetAsync<ScreeningJobDto>($"api/jobs/{jobId}/screening-jobs/latest");

    public Task<ApiResult<AiScoreDto>> GetScoreAsync(Guid applicationId)
        => api.GetAsync<AiScoreDto>($"api/applications/{applicationId}/ai-score");

    public Task<ApiResult<List<InterviewQuestionDto>>> GenerateQuestionsAsync(Guid applicationId)
        => api.PostAsync<List<InterviewQuestionDto>>($"api/applications/{applicationId}/questions", null);

    public Task<ApiResult> SaveQuestionsAsync(Guid interviewId, IReadOnlyList<InterviewQuestionDto> questions)
        => api.PutAsync($"api/interviews/{interviewId}/questions", new SaveQuestionsRequest(questions));

    public Task<ApiResult<List<InterviewQuestionDto>>> GetQuestionsAsync(Guid interviewId)
        => api.GetAsync<List<InterviewQuestionDto>>($"api/interviews/{interviewId}/questions");
}

public sealed class ReportApi(ApiClient api)
{
    public Task<ApiResult<DashboardDto>> GetDashboardAsync() => api.GetAsync<DashboardDto>("api/reports/dashboard");
}

public sealed class AdminApi(ApiClient api)
{
    public Task<ApiResult<List<UserDto>>> ListUsersAsync() => api.GetAsync<List<UserDto>>("api/admin/users");

    public Task<ApiResult> UpdateUserAsync(Guid id, UpdateUserRequest request)
        => api.PatchAsync($"api/admin/users/{id}", request);
}
