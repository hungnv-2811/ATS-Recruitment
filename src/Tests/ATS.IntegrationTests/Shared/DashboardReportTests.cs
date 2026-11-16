using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ATS.Persistence.Reporting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ATS.IntegrationTests.Shared;

/// <summary>Bao cao dashboard (TV1): phan tinh toan + phan quyen endpoint. SQL that kiem o pha tich hop.</summary>
public sealed class DashboardReportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Secret = "integration-test-secret-at-least-32-characters!!";
    private const string Issuer = "ats-recruitment";
    private const string Audience = "ats-recruitment-users";

    private readonly WebApplicationFactory<Program> _factory;

    public DashboardReportTests(WebApplicationFactory<Program> factory) => _factory = factory;

    // ---------------------------------------------------------------- tinh toan

    [Fact]
    public void Calculator_du_5_trang_thai_va_tinh_ti_le()
    {
        var dto = DashboardCalculator.Build(
            totalJobs: 3,
            openJobs: 2,
            applicationsByStatus: [("Applied", 5), ("Hired", 2), ("Rejected", 3)],
            topJobs: [new TopJobDto(Guid.NewGuid(), "Backend", 6)],
            aiScores: [("Scored", "OpenAI", 4), ("Scored", "Keyword", 1), ("Failed", null, 2), ("Pending", null, 9)],
            cacheEntries: 4,
            cacheHits: 12);

        Assert.Equal(10, dto.TotalApplications);
        Assert.Equal(["Applied", "Screening", "Interview", "Hired", "Rejected"], dto.ApplicationsByStatus.Keys);
        Assert.Equal(0, dto.ApplicationsByStatus["Screening"]);
        Assert.Equal(0.2, dto.HiredRate);
        Assert.Equal(5, dto.Ai.ScoredCount);
        Assert.Equal(2, dto.Ai.FailedCount);
        Assert.Equal(4, dto.Ai.ByAdapter["OpenAI"]);
        Assert.Equal(1, dto.Ai.ByAdapter["Keyword"]);
        Assert.Equal(0.75, dto.Ai.CacheHitRate);
    }

    [Fact]
    public void Calculator_khong_chia_cho_0_khi_chua_co_du_lieu()
    {
        var dto = DashboardCalculator.Build(0, 0, [], [], [], 0, 0);

        Assert.Equal(0, dto.TotalApplications);
        Assert.Equal(0d, dto.HiredRate);
        Assert.Equal(0d, dto.Ai.CacheHitRate);
        Assert.Empty(dto.Ai.ByAdapter);
    }

    [Fact]
    public void Sql_dung_ten_bang_trong_database_design()
    {
        foreach (var table in new[]
                 {
                     "recruitment.jobs", "recruitment.applications",
                     "aiscreening.ai_scores", "aiscreening.ai_score_cache", "hr_owner_id",
                 })
        {
            Assert.Contains(table, DashboardReportService.SqlText, StringComparison.Ordinal);
        }
    }

    // ---------------------------------------------------------------- endpoint

    private sealed class StubReports : IDashboardReportService
    {
        public List<Guid?> Calls { get; } = [];

        public Task<DashboardDto> GetDashboardAsync(Guid? hrOwnerUserId, CancellationToken ct = default)
        {
            Calls.Add(hrOwnerUserId);
            return Task.FromResult(DashboardCalculator.Build(1, 1, [("Applied", 1)], [], [], 0, 0));
        }
    }

    private (HttpClient Client, StubReports Stub) CreateClient()
    {
        var stub = new StubReports();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Jwt:Secret", Secret);
            builder.UseSetting("Jwt:Issuer", Issuer);
            builder.UseSetting("Jwt:Audience", Audience);
            builder.ConfigureTestServices(services =>
                services.Replace(ServiceDescriptor.Scoped<IDashboardReportService>(_ => stub)));
        }).CreateClient();
        return (client, stub);
    }

    private static string Token(Guid userId, string role)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(10),
            Claims = new Dictionary<string, object> { ["sub"] = userId.ToString(), ["role"] = role },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static async Task<HttpResponseMessage> GetDashboard(HttpClient client, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/reports/dashboard");
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Chua_dang_nhap_thi_401()
    {
        var (client, stub) = CreateClient();

        var response = await GetDashboard(client, token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(stub.Calls);
    }

    [Fact]
    public async Task Ung_vien_thi_403()
    {
        var (client, stub) = CreateClient();

        var response = await GetDashboard(client, Token(Guid.NewGuid(), "Candidate"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(stub.Calls);
    }

    [Fact]
    public async Task HR_chi_xem_tin_cua_minh()
    {
        var (client, stub) = CreateClient();
        var hrId = Guid.NewGuid();

        var response = await GetDashboard(client, Token(hrId, "HR"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([hrId], stub.Calls);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, json.RootElement.GetProperty("totalApplications").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("applicationsByStatus").GetProperty("Applied").GetInt32());
        Assert.True(json.RootElement.GetProperty("ai").TryGetProperty("cacheHitRate", out _));
    }

    [Fact]
    public async Task Admin_xem_toan_he_thong()
    {
        var (client, stub) = CreateClient();

        var response = await GetDashboard(client, Token(Guid.NewGuid(), "Admin"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([(Guid?)null], stub.Calls);
    }
}
