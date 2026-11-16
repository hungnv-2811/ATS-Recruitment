using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ATS.IntegrationTests;

/// <summary>
/// Endpoint AI (contracts.md muc 8.3) chay that qua pipeline HTTP: xac thuc, phan quyen theo vai (RB8),
/// han muc (RB2), va moi response co diem deu kem adapterUsed.
/// </summary>
/// <remarks>Test end-to-end nop don → Worker cham lam o pha tich hop sau khi merge nhanh TV2.</remarks>
public sealed class AiEndpointsTests : IClassFixture<AiApiFactory>
{
    private const string Candidate = "Candidate";
    private const string Hr = "HR";

    private readonly AiApiFactory _factory;

    public AiEndpointsTests(AiApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("POST", "/api/ai/score-preview")]
    [InlineData("GET", "/api/me/ai-quota")]
    [InlineData("POST", "/api/screening-jobs")]
    [InlineData("GET", "/api/screening-jobs/00000000-0000-0000-0000-000000000001")]
    [InlineData("GET", "/api/applications/00000000-0000-0000-0000-000000000001/ai-score")]
    [InlineData("POST", "/api/applications/00000000-0000-0000-0000-000000000001/questions")]
    [InlineData("GET", "/api/interviews/00000000-0000-0000-0000-000000000001/questions")]
    public async Task Chua_dang_nhap_thi_401(string method, string url)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HR_goi_preview_thi_403()
    {
        var client = _factory.ClientAs(Guid.NewGuid(), Hr);

        var response = await client.PostAsJsonAsync("/api/ai/score-preview", new { cvId = Guid.NewGuid(), jobId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/screening-jobs")]
    [InlineData("POST", "/api/applications/00000000-0000-0000-0000-000000000001/questions")]
    [InlineData("GET", "/api/jobs/00000000-0000-0000-0000-000000000001/screening-jobs/latest")]
    public async Task Ung_vien_goi_endpoint_cua_HR_thi_403(string method, string url)
    {
        var client = _factory.ClientAs(Guid.NewGuid(), Candidate);
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { jobId = Guid.NewGuid() });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Han_muc_hom_nay_cua_user_moi()
    {
        var client = _factory.ClientAs(Guid.NewGuid(), Candidate);

        var quota = await client.GetFromJsonAsync<JsonElement>("/api/me/ai-quota");

        Assert.Equal(0, quota.GetProperty("previewUsedToday").GetInt32());
        Assert.Equal(AiApiFactory.PreviewLimit, quota.GetProperty("previewLimitPerDay").GetInt32());
        Assert.Equal(AiApiFactory.PreviewLimit, quota.GetProperty("previewRemainingToday").GetInt32());
        Assert.True(quota.GetProperty("resetsAt").GetDateTimeOffset() > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Preview_tra_diem_kem_adapterUsed_lan_hai_trung_cache_khong_tru_luot()
    {
        var candidate = Guid.NewGuid();
        var cv = _factory.Recruitment.AddCv(candidate);
        var job = _factory.Recruitment.AddJob(Guid.NewGuid());
        var client = _factory.ClientAs(candidate, Candidate);
        var body = new { cvId = cv.CvId, jobId = job.JobId };

        var first = await client.PostAsJsonAsync("/api/ai/score-preview", body);
        var second = await client.PostAsJsonAsync("/api/ai/score-preview", body);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var a = await first.Content.ReadFromJsonAsync<JsonElement>();
        var b = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Fake", a.GetProperty("adapterUsed").GetString());
        Assert.False(a.GetProperty("cacheHit").GetBoolean());
        Assert.True(b.GetProperty("cacheHit").GetBoolean());
        Assert.Equal(a.GetProperty("score").GetInt32(), b.GetProperty("score").GetInt32());
        Assert.Equal(AiApiFactory.PreviewLimit - 1, b.GetProperty("previewRemainingToday").GetInt32());
    }

    [Fact]
    public async Task Het_luot_preview_thi_429_kem_so_luot_con_lai_va_thoi_diem_reset()
    {
        var candidate = Guid.NewGuid();
        var cv = _factory.Recruitment.AddCv(candidate);
        var client = _factory.ClientAs(candidate, Candidate);

        HttpResponseMessage? last = null;
        for (var i = 0; i <= AiApiFactory.PreviewLimit; i++)
        {
            var job = _factory.Recruitment.AddJob(Guid.NewGuid()); // moi lan mot tin khac → truot cache → tru luot
            last = await client.PostAsJsonAsync("/api/ai/score-preview", new { cvId = cv.CvId, jobId = job.JobId });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        var problem = await last.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("quota_exceeded", problem.GetProperty("code").GetString());
        Assert.Equal(0, problem.GetProperty("previewRemainingToday").GetInt32());
        Assert.True(problem.GetProperty("resetsAt").GetDateTimeOffset() > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Preview_CV_cua_nguoi_khac_thi_404()
    {
        var cv = _factory.Recruitment.AddCv(Guid.NewGuid());
        var job = _factory.Recruitment.AddJob(Guid.NewGuid());
        var client = _factory.ClientAs(Guid.NewGuid(), Candidate);

        var response = await client.PostAsJsonAsync("/api/ai/score-preview", new { cvId = cv.CvId, jobId = job.JobId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HR_chu_tin_chay_sang_loc_lo_202_roi_polling_duoc()
    {
        var hr = Guid.NewGuid();
        var job = _factory.Recruitment.AddJob(hr);
        _factory.Recruitment.AddApplication(job, _factory.Recruitment.AddCv(Guid.NewGuid()));
        _factory.Recruitment.AddApplication(job, _factory.Recruitment.AddCv(Guid.NewGuid()));
        var client = _factory.ClientAs(hr, Hr);

        var started = await client.PostAsJsonAsync("/api/screening-jobs", new { jobId = job.JobId });

        Assert.Equal(HttpStatusCode.Accepted, started.StatusCode);
        var dto = await started.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, dto.GetProperty("targetCount").GetInt32());
        Assert.Equal("Queued", dto.GetProperty("status").GetString());

        var id = dto.GetProperty("id").GetGuid();
        var polled = await client.GetFromJsonAsync<JsonElement>($"/api/screening-jobs/{id}");
        var latest = await client.GetFromJsonAsync<JsonElement>($"/api/jobs/{job.JobId}/screening-jobs/latest");
        Assert.Equal(id, polled.GetProperty("id").GetGuid());
        Assert.Equal(id, latest.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task HR_khac_khong_chay_duoc_sang_loc_tin_nguoi_khac()
    {
        var job = _factory.Recruitment.AddJob(Guid.NewGuid());
        var client = _factory.ClientAs(Guid.NewGuid(), Hr);

        var response = await client.PostAsJsonAsync("/api/screening-jobs", new { jobId = job.JobId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Diem_AI_cua_don_chua_cham_la_Pending_va_chi_chu_don_xem_duoc()
    {
        var candidate = Guid.NewGuid();
        var job = _factory.Recruitment.AddJob(Guid.NewGuid());
        var applicationId = _factory.Recruitment.AddApplication(job, _factory.Recruitment.AddCv(candidate));

        var own = await _factory.ClientAs(candidate, Candidate).GetAsync($"/api/applications/{applicationId}/ai-score");
        var stranger = await _factory.ClientAs(Guid.NewGuid(), Candidate).GetAsync($"/api/applications/{applicationId}/ai-score");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        var dto = await own.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Pending", dto.GetProperty("screeningStatus").GetString());
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("score").ValueKind);
        Assert.True(dto.TryGetProperty("adapterUsed", out _));
        Assert.Equal(HttpStatusCode.Forbidden, stranger.StatusCode);
    }

    [Fact]
    public async Task HR_sinh_cau_hoi_mau_khi_AiProvider_Fake()
    {
        var hr = Guid.NewGuid();
        var job = _factory.Recruitment.AddJob(hr);
        var applicationId = _factory.Recruitment.AddApplication(job, _factory.Recruitment.AddCv(Guid.NewGuid()));

        var response = await _factory.ClientAs(hr, Hr).PostAsync($"/api/applications/{applicationId}/questions", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var questions = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(10, questions.GetArrayLength());
        Assert.Equal("Template", questions[0].GetProperty("source").GetString());
    }
}
