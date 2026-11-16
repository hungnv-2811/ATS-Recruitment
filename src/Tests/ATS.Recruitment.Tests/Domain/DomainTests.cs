using ATS.Recruitment.Domain;
using Xunit;
using JobApplication = ATS.Recruitment.Domain.Application;

namespace ATS.Recruitment.Tests.Domain;

public sealed class ApplicationStateMachineTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 8, 0, 0, TimeSpan.Zero);

    private static JobApplication NewApplication() => JobApplication.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);

    [Fact]
    public void Don_moi_o_trang_thai_Applied()
        => Assert.Equal(ApplicationStatus.Applied, NewApplication().Status);

    [Fact]
    public void Di_het_nhanh_chinh_Applied_Screening_Interview_Hired()
    {
        var a = NewApplication();

        Assert.True(a.ChangeStatus(ApplicationStatus.Screening, null, Now).IsSuccess);
        Assert.True(a.ChangeStatus(ApplicationStatus.Interview, null, Now).IsSuccess);
        Assert.True(a.ChangeStatus(ApplicationStatus.Hired, "Nhan", Now.AddDays(1)).IsSuccess);

        Assert.Equal(ApplicationStatus.Hired, a.Status);
        Assert.Equal("Nhan", a.HrNote);
        Assert.Equal(Now.AddDays(1), a.UpdatedAt);
    }

    [Theory]
    [InlineData(ApplicationStatus.Hired)]
    [InlineData(ApplicationStatus.Interview)]
    [InlineData(ApplicationStatus.Applied)]
    public void Khong_nhay_coc_tu_Applied(ApplicationStatus target)
    {
        var a = NewApplication();

        var result = a.ChangeStatus(target, null, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
        Assert.Equal(ApplicationStatus.Applied, a.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Moi_trang_thai_chua_ket_thuc_deu_sang_duoc_Rejected(int stepsForward)
    {
        var a = NewApplication();
        var path = new[] { ApplicationStatus.Screening, ApplicationStatus.Interview };
        foreach (var step in path.Take(stepsForward))
        {
            a.ChangeStatus(step, null, Now);
        }

        Assert.True(a.ChangeStatus(ApplicationStatus.Rejected, "Khong phu hop", Now).IsSuccess);
        Assert.Equal(ApplicationStatus.Rejected, a.Status);
    }

    [Fact]
    public void Trang_thai_ket_thuc_khong_chuyen_tiep_duoc()
    {
        var a = NewApplication();
        a.ChangeStatus(ApplicationStatus.Rejected, null, Now);

        Assert.True(a.ChangeStatus(ApplicationStatus.Screening, null, Now).IsFailure);
        Assert.True(a.ChangeStatus(ApplicationStatus.Rejected, null, Now).IsFailure);
        Assert.True(a.EnterInterviewStage(Now).IsFailure);
    }

    [Theory]
    [InlineData(ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.Screening)]
    public void Hen_phong_van_dua_don_Applied_Screening_sang_Interview(ApplicationStatus start)
    {
        var a = NewApplication();
        if (start == ApplicationStatus.Screening)
        {
            a.ChangeStatus(ApplicationStatus.Screening, null, Now);
        }

        Assert.True(a.EnterInterviewStage(Now).IsSuccess);
        Assert.Equal(ApplicationStatus.Interview, a.Status);
    }
}

public sealed class JobTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 8, 0, 0, TimeSpan.Zero);

    private static Job NewJob(bool publish = false)
        => Job.Create(Guid.NewGuid(), "Backend .NET", "Mo ta", "C#", "Ha Noi", EmploymentType.FullTime, publish, Now).Value;

    [Fact]
    public void Tin_luu_nhap_la_Draft_chua_co_PostedAt()
    {
        var job = NewJob();

        Assert.Equal(JobStatus.Draft, job.Status);
        Assert.Null(job.PostedAt);
        Assert.False(job.IsOpen);
    }

    [Fact]
    public void Publish_chuyen_Draft_sang_Open_va_ghi_PostedAt()
    {
        var job = NewJob();

        Assert.True(job.Publish(Now.AddHours(1)).IsSuccess);

        Assert.Equal(JobStatus.Open, job.Status);
        Assert.Equal(Now.AddHours(1), job.PostedAt);
    }

    [Fact]
    public void Publish_tin_khong_phai_Draft_bi_tu_choi()
    {
        var job = NewJob(publish: true);

        Assert.True(job.Publish(Now).IsFailure);
    }

    [Fact]
    public void Close_tin_Open_roi_khong_sua_khong_dong_lai_duoc()
    {
        var job = NewJob(publish: true);

        Assert.True(job.Close(Now.AddDays(3)).IsSuccess);
        Assert.Equal(JobStatus.Closed, job.Status);
        Assert.Equal(Now.AddDays(3), job.ClosedAt);

        Assert.True(job.Close(Now).IsFailure);
        Assert.True(job.Publish(Now).IsFailure);
        Assert.True(job.Update("Tieu de moi", "Mo ta", "", "", EmploymentType.PartTime).IsFailure);
    }

    [Fact]
    public void Tao_tin_thieu_tieu_de_bi_tu_choi()
    {
        var result = Job.Create(Guid.NewGuid(), " ", "Mo ta", "", "", EmploymentType.FullTime, true, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }
}
