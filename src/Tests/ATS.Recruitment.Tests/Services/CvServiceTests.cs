using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Domain;
using ATS.Recruitment.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATS.Recruitment.Tests.Services;

public sealed class CvServiceTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task CV_dau_tien_tu_thanh_mac_dinh_CV_sau_thi_khong()
    {
        var candidate = await _app.RegisterAsync("c@example.com");

        var first = await _app.UploadCvAsync(candidate.UserId, "CV 1");
        _app.Clock.Advance(TimeSpan.FromMinutes(1));
        var second = await _app.UploadCvAsync(candidate.UserId, "CV 2", "cv2.DOCX");

        Assert.True(first.IsDefault);
        Assert.False(second.IsDefault);
        Assert.Equal("docx", second.FileType);
        Assert.Equal(2, second.FileSizeKb);
        Assert.True(second.HasText);

        var list = await _app.Service<ICvService>().ListAsync(candidate.UserId);
        Assert.Equal(2, list.Value.Count);
        Assert.Single(list.Value, c => c.IsDefault);
    }

    [Fact]
    public async Task Upload_luu_file_va_text_trich_dong_bo()
    {
        var candidate = await _app.RegisterAsync("c@example.com");
        _app.Extractor.Text = "Kinh nghiem ASP.NET Core";

        var cv = await _app.UploadCvAsync(candidate.UserId);

        Assert.Single(_app.Files.Files);
        var stored = await _app.Db().Set<Cv>().SingleAsync();
        Assert.Equal(cv.Id, stored.Id);
        Assert.Equal("Kinh nghiem ASP.NET Core", stored.ExtractedText);
    }

    [Fact]
    public async Task Khong_trich_duoc_chu_van_upload_duoc_HasText_false()
    {
        var candidate = await _app.RegisterAsync("c@example.com");
        _app.Extractor.Text = string.Empty;

        var cv = await _app.UploadCvAsync(candidate.UserId);

        Assert.False(cv.HasText);
    }

    [Theory]
    [InlineData("cv.txt")]
    [InlineData("cv.doc")]
    [InlineData("cv")]
    public async Task Sai_dinh_dang_bi_tu_choi(string fileName)
    {
        var candidate = await _app.RegisterAsync("c@example.com");
        using var content = new MemoryStream(new byte[100]);

        var result = await _app.Service<ICvService>()
            .UploadAsync(candidate.UserId, new UploadCvCommand("CV", fileName, 100, content));

        Assert.Equal("validation", result.Error.Code);
        Assert.Empty(_app.Files.Files);
    }

    [Fact]
    public async Task File_lon_hon_5MB_bi_tu_choi()
    {
        var candidate = await _app.RegisterAsync("c@example.com");
        const int size = (5 * 1024 * 1024) + 1;
        using var content = new MemoryStream(new byte[size]);

        var result = await _app.Service<ICvService>()
            .UploadAsync(candidate.UserId, new UploadCvCommand("CV", "cv.pdf", size, content));

        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task Xoa_CV_da_dung_nop_don_tra_Conflict()
    {
        var hr = await _app.RegisterAsync("hr@example.com", "HR");
        var candidate = await _app.RegisterAsync("c@example.com");
        var job = await _app.CreateJobAsync(hr.UserId);
        var cv = await _app.UploadCvAsync(candidate.UserId);
        await _app.ApplyAsync(candidate.UserId, job.Id, cv.Id);

        var result = await _app.Service<ICvService>().DeleteAsync(candidate.UserId, cv.Id);

        Assert.Equal("conflict", result.Error.Code);
        Assert.Single(_app.Db().Set<Cv>());
    }

    [Fact]
    public async Task Xoa_CV_mac_dinh_thi_CV_moi_nhat_con_lai_thanh_mac_dinh()
    {
        var candidate = await _app.RegisterAsync("c@example.com");
        var first = await _app.UploadCvAsync(candidate.UserId, "CV 1");
        _app.Clock.Advance(TimeSpan.FromMinutes(1));
        var second = await _app.UploadCvAsync(candidate.UserId, "CV 2");

        var result = await _app.Service<ICvService>().DeleteAsync(candidate.UserId, first.Id);

        Assert.True(result.IsSuccess);
        var remaining = await _app.Db().Set<Cv>().SingleAsync();
        Assert.Equal(second.Id, remaining.Id);
        Assert.True(remaining.IsDefault);
        Assert.Single(_app.Files.Files);
    }

    [Fact]
    public async Task Dat_mac_dinh_chi_con_mot_CV_mac_dinh()
    {
        var candidate = await _app.RegisterAsync("c@example.com");
        await _app.UploadCvAsync(candidate.UserId, "CV 1");
        var second = await _app.UploadCvAsync(candidate.UserId, "CV 2");

        var result = await _app.Service<ICvService>().SetDefaultAsync(candidate.UserId, second.Id);

        Assert.True(result.IsSuccess);
        var defaults = await _app.Db().Set<Cv>().Where(c => c.IsDefault).ToListAsync();
        Assert.Equal(second.Id, Assert.Single(defaults).Id);
    }

    [Fact]
    public async Task Khong_thao_tac_duoc_CV_cua_nguoi_khac()
    {
        var owner = await _app.RegisterAsync("owner@example.com");
        var other = await _app.RegisterAsync("other@example.com");
        var cv = await _app.UploadCvAsync(owner.UserId);

        var delete = await _app.Service<ICvService>().DeleteAsync(other.UserId, cv.Id);
        var setDefault = await _app.Service<ICvService>().SetDefaultAsync(other.UserId, cv.Id);

        Assert.Equal("not_found", delete.Error.Code);
        Assert.Equal("not_found", setDefault.Error.Code);
    }
}
