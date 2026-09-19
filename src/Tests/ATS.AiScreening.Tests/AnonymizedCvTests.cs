using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Services;
using Xunit;

namespace ATS.AiScreening.Tests;

public sealed class SimpleAnonymizerTests
{
    private readonly SimpleAnonymizer _anonymizer = new();

    [Fact]
    public async Task Go_bo_email_khoi_CV()
    {
        var result = await _anonymizer.AnonymizeAsync("Lien he: an.nguyen@example.com");

        Assert.DoesNotContain("an.nguyen@example.com", result.Text, StringComparison.Ordinal);
        Assert.Contains("[EMAIL]", result.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0912345678")]
    [InlineData("+84912345678")]
    public async Task Go_bo_so_dien_thoai_Viet_Nam(string phone)
    {
        var result = await _anonymizer.AnonymizeAsync($"SDT {phone} lien he");

        Assert.DoesNotContain(phone, result.Text, StringComparison.Ordinal);
        Assert.Contains("[PHONE]", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Giu_lai_ky_nang_va_kinh_nghiem()
    {
        var result = await _anonymizer.AnonymizeAsync(
            "Email: a@b.com. 5 nam kinh nghiem C#, PostgreSQL, Docker.");

        // An danh KHONG duoc lam mat thong tin dung de cham diem.
        Assert.Contains("PostgreSQL", result.Text, StringComparison.Ordinal);
        Assert.Contains("Docker", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tu_choi_CV_rong()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _anonymizer.AnonymizeAsync("   "));
    }
}

public sealed class CacheKeyTests
{
    [Fact]
    public void Cung_noi_dung_cho_cung_khoa()
    {
        var a = CacheKey.From("CV text", "JD text", "gpt-4o-mini", "scoring-v1");
        var b = CacheKey.From("CV text", "JD text", "gpt-4o-mini", "scoring-v1");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Khac_hoa_thuong_hay_khoang_trang_van_trung_cache()
    {
        // Neu khong chuan hoa thi moi lan format lai CV la mot lan tra tien vo ich (RB2).
        var a = CacheKey.From("CV  Text", "JD text", "gpt-4o-mini", "scoring-v1");
        var b = CacheKey.From("cv text", "JD TEXT", "gpt-4o-mini", "scoring-v1");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Doi_prompt_version_thi_doi_khoa()
    {
        // Doi prompt ma van tra cache cu la tra ve ket qua cua prompt da bo di.
        var a = CacheKey.From("CV", "JD", "gpt-4o-mini", "scoring-v1");
        var b = CacheKey.From("CV", "JD", "gpt-4o-mini", "scoring-v2");

        Assert.NotEqual(a, b);
    }
}

public sealed class ScoreTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(78)]
    [InlineData(100)]
    public void Chap_nhan_diem_trong_khoang_hop_le(int value)
        => Assert.Equal(value, new Score(value).Value);

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Tu_choi_diem_ngoai_khoang(int value)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Score(value));
}
