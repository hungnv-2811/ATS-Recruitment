using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Tests.Support;
using Xunit;

namespace ATS.Recruitment.Tests.Services;

public sealed class JobServiceTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task Danh_sach_cong_khai_chi_co_tin_Open_va_loc_theo_tu_khoa()
    {
        var hr = await _app.RegisterAsync("hr@example.com", "HR");
        await _app.CreateJobAsync(hr.UserId, publish: true, title: "Backend .NET");
        await _app.CreateJobAsync(hr.UserId, publish: true, title: "Frontend React");
        await _app.CreateJobAsync(hr.UserId, publish: false, title: "Backend Draft");

        var all = (await _app.Service<IJobService>().SearchAsync(new JobSearchQuery(null, null, null))).Value;
        var backend = (await _app.Service<IJobService>().SearchAsync(new JobSearchQuery("backend", "ha noi", "fulltime"))).Value;

        Assert.Equal(2, all.Total);
        Assert.All(all.Items, j => Assert.Equal("Open", j.Status));
        Assert.Equal("Backend .NET", Assert.Single(backend.Items).Title);
    }

    [Fact]
    public async Task Tin_Draft_chi_chu_tin_xem_duoc()
    {
        var hr = await _app.RegisterAsync("hr@example.com", "HR");
        var draft = await _app.CreateJobAsync(hr.UserId, publish: false);

        var anonymous = await _app.Service<IJobService>().GetAsync(draft.Id, null);
        var owner = await _app.Service<IJobService>().GetAsync(draft.Id, hr.UserId);

        Assert.Equal("not_found", anonymous.Error.Code);
        Assert.Equal("Draft", owner.Value.Status);
        Assert.Null(owner.Value.PostedAt);
    }

    [Fact]
    public async Task HR_khac_khong_sua_dong_dang_duoc_tin()
    {
        var hr = await _app.RegisterAsync("hr@example.com", "HR");
        var other = await _app.RegisterAsync("hr2@example.com", "HR");
        var job = await _app.CreateJobAsync(hr.UserId, publish: false);
        var service = _app.Service<IJobService>();

        Assert.Equal("forbidden", (await service.PublishAsync(other.UserId, job.Id)).Error.Code);
        Assert.Equal("forbidden", (await service.CloseAsync(other.UserId, job.Id)).Error.Code);
        Assert.Equal("forbidden", (await service.UpdateAsync(other.UserId, job.Id, new UpdateJobRequest("X", null, null, null, null))).Error.Code);
    }

    [Fact]
    public async Task Publish_roi_sua_tin_va_dem_so_don()
    {
        var hr = await _app.RegisterAsync("hr@example.com", "HR");
        var job = await _app.CreateJobAsync(hr.UserId, publish: false);

        Assert.True((await _app.Service<IJobService>().PublishAsync(hr.UserId, job.Id)).IsSuccess);
        var updated = await _app.Service<IJobService>()
            .UpdateAsync(hr.UserId, job.Id, new UpdateJobRequest("Senior Backend", null, null, null, "Contract"));

        Assert.Equal("Senior Backend", updated.Value.Title);
        Assert.Equal("Contract", updated.Value.EmploymentType);
        Assert.Equal("Open", updated.Value.Status);
        Assert.Equal(_app.Clock.Now, updated.Value.PostedAt);
        Assert.Equal(0, updated.Value.ApplicationCount);
    }

    [Fact]
    public async Task EmploymentType_la_bi_tu_choi()
    {
        var hr = await _app.RegisterAsync("hr@example.com", "HR");

        var result = await _app.Service<IJobService>()
            .CreateAsync(hr.UserId, new CreateJobRequest("T", "D", null, null, "Freelance", true));

        Assert.Equal("validation", result.Error.Code);
    }
}
