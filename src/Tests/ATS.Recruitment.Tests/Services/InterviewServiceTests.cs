using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Domain;
using ATS.Recruitment.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;
using JobApplication = ATS.Recruitment.Domain.Application;

namespace ATS.Recruitment.Tests.Services;

public sealed class InterviewServiceTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task SMTP_loi_van_luu_buoi_phong_van_EmailSent_false_va_tra_thanh_cong()
    {
        var (hr, applicationId) = await SetupAsync();
        _app.Email.Fail = true;

        var result = await _app.Service<IInterviewService>().ScheduleAsync(hr, applicationId, Request());

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.EmailSent);
        Assert.Equal("Scheduled", result.Value.Status);

        var saved = await _app.Db().Set<Interview>().SingleAsync();
        Assert.Equal(result.Value.Id, saved.Id);
        Assert.False(saved.EmailSent);
    }

    [Fact]
    public async Task Hen_phong_van_gui_loi_moi_va_dua_don_Applied_sang_Interview()
    {
        var (hr, applicationId) = await SetupAsync();

        var result = await _app.Service<IInterviewService>().ScheduleAsync(hr, applicationId, Request());

        Assert.True(result.Value.EmailSent);
        Assert.True((await _app.Db().Set<Interview>().SingleAsync()).EmailSent);
        var mail = Assert.Single(_app.Email.Sent);
        Assert.Equal("c@example.com", mail.ToAddress);
        Assert.Contains("Backend .NET", mail.Subject, StringComparison.Ordinal);
        Assert.StartsWith("Mời phỏng vấn", mail.Subject, StringComparison.Ordinal);
        Assert.Contains("giờ Việt Nam", mail.HtmlBody, StringComparison.Ordinal);
        Assert.Equal(ApplicationStatus.Interview, (await _app.Db().Set<JobApplication>().SingleAsync()).Status);
    }

    [Fact]
    public async Task Gui_lai_loi_moi_SMTP_van_loi_tra_External_roi_thanh_cong_khi_SMTP_song_lai()
    {
        var (hr, applicationId) = await SetupAsync();
        _app.Email.Fail = true;
        var interview = (await _app.Service<IInterviewService>().ScheduleAsync(hr, applicationId, Request())).Value;

        var stillDown = await _app.Service<IInterviewService>().ResendInviteAsync(hr, interview.Id);
        _app.Email.Fail = false;
        var back = await _app.Service<IInterviewService>().ResendInviteAsync(hr, interview.Id);

        Assert.Equal("external_service", stillDown.Error.Code);
        Assert.True(back.IsSuccess);
        Assert.True((await _app.Db().Set<Interview>().SingleAsync()).EmailSent);
    }

    [Fact]
    public async Task HR_khac_khong_hen_duoc_phong_van()
    {
        var (_, applicationId) = await SetupAsync();
        var otherHr = await _app.RegisterAsync("hr2@example.com", "HR");

        var result = await _app.Service<IInterviewService>().ScheduleAsync(otherHr.UserId, applicationId, Request());

        Assert.Equal("forbidden", result.Error.Code);
        Assert.Empty(_app.Db().Set<Interview>());
    }

    [Fact]
    public async Task Thoi_gian_trong_qua_khu_hoac_hinh_thuc_la_bi_tu_choi()
    {
        var (hr, applicationId) = await SetupAsync();
        var service = _app.Service<IInterviewService>();

        var past = await service.ScheduleAsync(hr, applicationId, Request() with { ScheduledAt = _app.Clock.Now.AddHours(-1) });
        var badMode = await service.ScheduleAsync(hr, applicationId, Request() with { Mode = "phone" });

        Assert.Equal("validation", past.Error.Code);
        Assert.Equal("validation", badMode.Error.Code);
    }

    [Fact]
    public async Task Danh_gia_chuyen_buoi_phong_van_sang_Done_va_chi_mot_lan()
    {
        var (hr, applicationId) = await SetupAsync();
        var interview = (await _app.Service<IInterviewService>().ScheduleAsync(hr, applicationId, Request())).Value;

        var first = await _app.Service<IInterviewService>()
            .SubmitEvaluationAsync(hr, interview.Id, new SubmitEvaluationRequest(4, "Tot", "hire"));
        var second = await _app.Service<IInterviewService>()
            .SubmitEvaluationAsync(hr, interview.Id, new SubmitEvaluationRequest(3, "Lai", "Consider"));

        Assert.True(first.IsSuccess);
        Assert.Equal("Hire", first.Value.Recommendation);
        Assert.Equal("conflict", second.Error.Code);

        var listed = (await _app.Service<IInterviewService>().ListByApplicationAsync(hr, applicationId)).Value;
        var only = Assert.Single(listed);
        Assert.Equal("Done", only.Status);
        Assert.Equal(4, only.Evaluation!.Rating);
    }

    [Theory]
    [InlineData(0, "Hire")]
    [InlineData(6, "Hire")]
    [InlineData(3, "Maybe")]
    public async Task Danh_gia_sai_diem_hoac_de_xuat_bi_tu_choi(int rating, string recommendation)
    {
        var (hr, applicationId) = await SetupAsync();
        var interview = (await _app.Service<IInterviewService>().ScheduleAsync(hr, applicationId, Request())).Value;

        var result = await _app.Service<IInterviewService>()
            .SubmitEvaluationAsync(hr, interview.Id, new SubmitEvaluationRequest(rating, null, recommendation));

        Assert.Equal("validation", result.Error.Code);
        Assert.Equal(InterviewStatus.Scheduled, (await _app.Db().Set<Interview>().SingleAsync()).Status);
    }

    [Fact]
    public async Task Lich_phong_van_sap_toi_cua_HR()
    {
        var (hr, applicationId) = await SetupAsync();
        await _app.Service<IInterviewService>().ScheduleAsync(hr, applicationId, Request());

        var upcoming = (await _app.Service<IInterviewService>().ListUpcomingForHrAsync(hr)).Value;

        var item = Assert.Single(upcoming);
        Assert.Equal("Backend .NET", item.JobTitle);
        Assert.Equal("Ung Vien", item.CandidateName);
        Assert.Equal("online", item.Mode);
    }

    private ScheduleInterviewRequest Request()
        => new(_app.Clock.Now.AddDays(2), "Online", "https://meet.example.com/abc", "Pham Interviewer");

    private async Task<(Guid Hr, Guid ApplicationId)> SetupAsync()
    {
        var hr = await _app.RegisterAsync("hr@example.com", "HR", "HR Chu Tin");
        var candidate = await _app.RegisterAsync("c@example.com", "Candidate", "Ung Vien");
        var job = await _app.CreateJobAsync(hr.UserId);
        var cv = await _app.UploadCvAsync(candidate.UserId);
        var application = await _app.ApplyAsync(candidate.UserId, job.Id, cv.Id);
        return (hr.UserId, application.Id);
    }
}
