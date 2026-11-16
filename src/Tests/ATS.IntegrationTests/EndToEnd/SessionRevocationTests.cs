using System.Net;
using ATS.Identity.Application;
using ATS.Identity.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ATS.IntegrationTests.EndToEnd;

/// <summary>
/// Qua HTTP that: token van dung chu ky va con han nhung tai khoan da bi khoa/doi vai → 401.
/// </summary>
public sealed class SessionRevocationTests : IClassFixture<E2eApiFactory>
{
    private readonly E2eApiFactory _factory;

    public SessionRevocationTests(E2eApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Khoa_tai_khoan_thi_token_dang_dung_bi_401_ngay()
    {
        var (client, userId, _) = await _factory.RegisterAsync("Candidate", "Ung Vien Bi Khoa");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/me")).StatusCode);

        await UpdateUserAsync(userId, new UpdateUserRequest(IsActive: false, Role: null));

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me/cvs")).StatusCode);
    }

    [Fact]
    public async Task Doi_vai_thi_token_mang_vai_cu_bi_401_khong_dung_duoc_quyen_cu()
    {
        var (client, userId, _) = await _factory.RegisterAsync("HR", "HR Bi Ha Vai");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/hr/jobs")).StatusCode);

        await UpdateUserAsync(userId, new UpdateUserRequest(IsActive: null, Role: "Candidate"));

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/hr/jobs")).StatusCode);
    }

    [Fact]
    public async Task Tai_khoan_khac_khong_bi_anh_huong()
    {
        var (locked, lockedId, _) = await _factory.RegisterAsync("Candidate", "Bi Khoa");
        var (other, _, _) = await _factory.RegisterAsync("Candidate", "Binh Thuong");

        await UpdateUserAsync(lockedId, new UpdateUserRequest(IsActive: false, Role: null));

        Assert.Equal(HttpStatusCode.Unauthorized, (await locked.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/api/me")).StatusCode);
    }

    private async Task UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        // Goi thang service Admin (khong qua HTTP) — test nay kiem phia BEN KIA: token cu bi tu choi.
        await using var scope = _factory.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IUserAdminService>()
            .UpdateUserAsync(Guid.NewGuid(), userId, request);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
    }
}
