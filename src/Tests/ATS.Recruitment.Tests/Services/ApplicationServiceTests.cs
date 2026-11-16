using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Application.Ports;
using ATS.Recruitment.Domain;
using ATS.Recruitment.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;
using JobApplication = ATS.Recruitment.Domain.Application;

namespace ATS.Recruitment.Tests.Services;

public sealed class ApplicationServiceTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task Nop_don_thanh_cong_tao_don_Applied_va_nho_AI_cham()
    {
        var (hr, candidate, job, cv) = await SetupAsync();

        var dto = await _app.ApplyAsync(candidate, job.Id, cv.Id);

        Assert.Equal("Applied", dto.Status);
        Assert.Equal("Pending", dto.ScreeningStatus);
        Assert.Equal(job.Title, dto.JobTitle);
        Assert.Equal(cv.Name, dto.CvName);
        Assert.Null(dto.AiScore);
        Assert.Equal(dto.Id, Assert.Single(_app.Trigger.Requested));
        Assert.NotEqual(Guid.Empty, hr);
    }

    [Fact]
    public async Task Nop_trung_tin_tra_Conflict()
    {
        var (_, candidate, job, cv) = await SetupAsync();
        await _app.ApplyAsync(candidate, job.Id, cv.Id);

        var again = await _app.Service<IApplicationService>().ApplyAsync(candidate, new ApplyRequest(job.Id, cv.Id));

        Assert.Equal("conflict", again.Error.Code);
        Assert.Single(_app.Db().Set<JobApplication>());
    }

    [Fact]
    public async Task Nop_bang_CV_cua_nguoi_khac_bi_tu_choi()
    {
        var (_, _, job, cvOfSomeoneElse) = await SetupAsync();
        var intruder = await _app.RegisterAsync("intruder@example.com");

        var result = await _app.Service<IApplicationService>()
            .ApplyAsync(intruder.UserId, new ApplyRequest(job.Id, cvOfSomeoneElse.Id));

        Assert.Equal("not_found", result.Error.Code);
        Assert.Empty(_app.Db().Set<JobApplication>());
    }

    [Fact]
    public async Task Nop_vao_tin_da_dong_tra_Validation()
    {
        var (hr, candidate, job, cv) = await SetupAsync();
        Assert.True((await _app.Service<IJobService>().CloseAsync(hr, job.Id)).IsSuccess);

        var result = await _app.Service<IApplicationService>().ApplyAsync(candidate, new ApplyRequest(job.Id, cv.Id));

        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task Nop_vao_tin_Draft_tra_Validation()
    {
        var hr = await _app.RegisterAsync("hr@example.com", "HR");
        var candidate = await _app.RegisterAsync("c@example.com");
        var draft = await _app.CreateJobAsync(hr.UserId, publish: false);
        var cv = await _app.UploadCvAsync(candidate.UserId);

        var result = await _app.Service<IApplicationService>().ApplyAsync(candidate.UserId, new ApplyRequest(draft.Id, cv.Id));

        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task Trigger_AI_loi_van_nop_don_thanh_cong_RB9()
    {
        var (_, candidate, job, cv) = await SetupAsync();
        _app.Trigger.Fail = true;

        var result = await _app.Service<IApplicationService>().ApplyAsync(candidate, new ApplyRequest(job.Id, cv.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(result.Value.Id, (await _app.Db().Set<JobApplication>().SingleAsync()).Id);
    }

    [Fact]
    public async Task Danh_sach_HR_xep_diem_giam_dan_chua_cham_o_cuoi()
    {
        var hr = await _app.RegisterAsync("hr@example.com", "HR");
        var job = await _app.CreateJobAsync(hr.UserId);
        var ids = new List<Guid>();
        foreach (var name in new[] { "Chua Cham", "Diem Thap", "Diem Cao" })
        {
            var c = await _app.RegisterAsync($"{name.Replace(' ', '.')}@example.com", fullName: name);
            var cv = await _app.UploadCvAsync(c.UserId);
            ids.Add((await _app.ApplyAsync(c.UserId, job.Id, cv.Id)).Id);
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
        }

        _app.Scores.Scores[ids[1]] = new ApplicationAiScore(40, "Thieu kinh nghiem", "Keyword", "Scored");
        _app.Scores.Scores[ids[2]] = new ApplicationAiScore(85, "Phu hop", "OpenAI", "Scored");

        var ranked = (await _app.Service<IApplicationService>().ListForJobAsync(hr.UserId, job.Id, null)).Value;

        Assert.Equal(new[] { "Diem Cao", "Diem Thap", "Chua Cham" }, ranked.Select(r => r.CandidateName));
        Assert.Equal("OpenAI", ranked[0].AiAdapterUsed);
        Assert.Equal("Pending", ranked[2].ScreeningStatus);
        Assert.Null(ranked[2].AiScore);
    }

    [Fact]
    public async Task HR_khong_so_huu_tin_khong_xem_duoc_danh_sach()
    {
        var (_, _, job, _) = await SetupAsync();
        var otherHr = await _app.RegisterAsync("hr2@example.com", "HR");

        var result = await _app.Service<IApplicationService>().ListForJobAsync(otherHr.UserId, job.Id, null);

        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task Chuyen_trang_thai_dung_luat_va_sai_luat()
    {
        var (hr, candidate, job, cv) = await SetupAsync();
        var app = await _app.ApplyAsync(candidate, job.Id, cv.Id);
        var service = _app.Service<IApplicationService>();

        var skip = await service.ChangeStatusAsync(hr, app.Id, new ChangeStatusRequest("Hired", null));
        var ok = await _app.Service<IApplicationService>().ChangeStatusAsync(hr, app.Id, new ChangeStatusRequest("screening", "CV tot"));

        Assert.Equal("validation", skip.Error.Code);
        Assert.True(ok.IsSuccess);
        var saved = await _app.Db().Set<JobApplication>().SingleAsync();
        Assert.Equal(ApplicationStatus.Screening, saved.Status);
        Assert.Equal("CV tot", saved.HrNote);
    }

    [Fact]
    public async Task Ung_vien_xem_chi_tiet_don_cua_minh_khong_thay_ghi_chu_HR()
    {
        var (hr, candidate, job, cv) = await SetupAsync();
        var app = await _app.ApplyAsync(candidate, job.Id, cv.Id);
        await _app.Service<IApplicationService>().ChangeStatusAsync(hr, app.Id, new ChangeStatusRequest("Screening", "Ghi chu noi bo"));

        var asCandidate = await _app.Service<IApplicationService>().GetDetailAsync(candidate, app.Id);
        var asHr = await _app.Service<IApplicationService>().GetDetailAsync(hr, app.Id);
        var stranger = await _app.RegisterAsync("stranger@example.com");
        var asStranger = await _app.Service<IApplicationService>().GetDetailAsync(stranger.UserId, app.Id);

        Assert.Null(asCandidate.Value.HrNote);
        Assert.Equal("Ghi chu noi bo", asHr.Value.HrNote);
        Assert.Equal("c@example.com", asHr.Value.CandidateEmail);
        Assert.Equal("forbidden", asStranger.Error.Code);
    }

    [Fact]
    public async Task Lich_su_ung_tuyen_cua_ung_vien_kem_diem_AI()
    {
        var (_, candidate, job, cv) = await SetupAsync();
        var app = await _app.ApplyAsync(candidate, job.Id, cv.Id);
        _app.Scores.Scores[app.Id] = new ApplicationAiScore(70, "Kha", "Fake", "Scored");

        var mine = (await _app.Service<IApplicationService>().ListMineAsync(candidate)).Value;

        var only = Assert.Single(mine);
        Assert.Equal(70, only.AiScore);
        Assert.Equal("Scored", only.ScreeningStatus);
    }

    [Fact]
    public async Task IRecruitmentQueries_tra_snapshot_cho_AiScreening()
    {
        var (hr, candidate, job, cv) = await SetupAsync();
        var app = await _app.ApplyAsync(candidate, job.Id, cv.Id);

        var snapshot = await _app.Service<IRecruitmentQueries>().GetApplicationAsync(app.Id);

        Assert.NotNull(snapshot);
        Assert.Equal("Applied", snapshot.Status);
        Assert.Equal(hr, snapshot.Job.HrOwnerUserId);
        Assert.Equal("Open", snapshot.Job.Status);
        Assert.Equal(candidate, snapshot.Cv.CandidateUserId);
        Assert.False(string.IsNullOrEmpty(snapshot.Cv.ExtractedText));
        Assert.Equal([app.Id], await _app.Service<IRecruitmentQueries>().ListApplicationIdsByJobAsync(job.Id));
    }

    private async Task<(Guid Hr, Guid Candidate, JobDto Job, CvDto Cv)> SetupAsync()
    {
        var hr = await _app.RegisterAsync("hr@example.com", "HR", "HR Chu Tin");
        var candidate = await _app.RegisterAsync("c@example.com", "Candidate", "Ung Vien");
        var job = await _app.CreateJobAsync(hr.UserId);
        var cv = await _app.UploadCvAsync(candidate.UserId);
        return (hr.UserId, candidate.UserId, job, cv);
    }
}
