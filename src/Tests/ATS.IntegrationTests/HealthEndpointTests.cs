using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ATS.IntegrationTests;

/// <summary>
/// Moc deliverable cua tuan 2: API khoi dong duoc va tra 200 o /health.
/// </summary>
/// <remarks>
/// Test nay KHONG can database: /health khong cham DbContext, va Npgsql chi mo
/// ket noi khi co query dau tien. Nho vay CI khong phai dung container Postgres
/// chi de biet API co boot duoc hay khong.
/// </remarks>
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_tra_ve_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_noi_ro_dang_chay_adapter_AI_nao()
    {
        // AiProvider cua api va worker lech nhau la rui ro da ghi trong weekly-plan:
        // ung vien nhan diem gia trong khi HR nhan diem that. Lo ra o /health de
        // nhin mot cai la biet, khong phai doc log container.
        var client = _factory.CreateClient();

        var body = await client.GetStringAsync("/health");

        Assert.Contains("aiProvider", body, StringComparison.OrdinalIgnoreCase);
    }
}
