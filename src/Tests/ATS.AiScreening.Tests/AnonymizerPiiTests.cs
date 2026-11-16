using ATS.AiScreening.Domain.Ports;
using ATS.AiScreening.Domain.Services;
using Xunit;

namespace ATS.AiScreening.Tests;

/// <summary>RB3: khong PII nao duoc toi LLM. Moi dang viet thuong gap trong CV Viet deu phai bi go.</summary>
public sealed class AnonymizerPiiTests
{
    private readonly SimpleAnonymizer _anonymizer = new();

    [Theory]
    [InlineData("0912345678")]
    [InlineData("0912 345 678")]
    [InlineData("0912.345.678")]
    [InlineData("091-234-5678")]
    [InlineData("+84 912 345 678")]
    [InlineData("(+84) 912345678")]
    [InlineData("84912345678")]
    [InlineData("028 3812 3456")]
    public async Task Go_SDT_Viet_Nam_nhieu_dinh_dang(string phone)
    {
        var result = await _anonymizer.AnonymizeAsync($"Điện thoại: {phone}. Kỹ năng C#");

        Assert.DoesNotContain(phone, result.Text, StringComparison.Ordinal);
        Assert.Contains("[PHONE]", result.Text, StringComparison.Ordinal);
        Assert.Contains("C#", result.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("an.nguyen@example.com")]
    [InlineData("tran_binh+cv@gmail.com.vn")]
    public async Task Go_email(string email)
    {
        var result = await _anonymizer.AnonymizeAsync($"Email: {email}");

        Assert.DoesNotContain(email, result.Text, StringComparison.Ordinal);
        Assert.Contains("[EMAIL]", result.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://www.facebook.com/an.nguyen.92")]
    [InlineData("fb.com/annguyen")]
    [InlineData("linkedin.com/in/an-nguyen-123")]
    [InlineData("https://vn.linkedin.com/in/binhtran")]
    [InlineData("github.com/annguyen")]
    public async Task Go_link_mang_xa_hoi(string link)
    {
        var result = await _anonymizer.AnonymizeAsync($"Liên hệ: {link} | Kỹ năng: Docker");

        Assert.DoesNotContain(link, result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[LINK]", result.Text, StringComparison.Ordinal);
        Assert.Contains("Docker", result.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Địa chỉ: 12 Nguyễn Trãi, Quận 1, TP.HCM", "Nguyễn Trãi")]
    [InlineData("Address: 45 Le Loi Street, District 3", "Le Loi")]
    [InlineData("Nơi ở: Cầu Giấy, Hà Nội", "Cầu Giấy")]
    [InlineData("Số 12 ngõ 34 đường Láng, Đống Đa", "Láng")]
    public async Task Go_dia_chi(string line, string secret)
    {
        var result = await _anonymizer.AnonymizeAsync($"{line}\nKỹ năng: PostgreSQL");

        Assert.DoesNotContain(secret, result.Text, StringComparison.Ordinal);
        Assert.Contains("[ADDRESS]", result.Text, StringComparison.Ordinal);
        Assert.Contains("PostgreSQL", result.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Trường Đại học Bách Khoa Hà Nội - chuyên ngành CNTT", "Bách Khoa")]
    [InlineData("Dai hoc Cong nghe, DHQGHN", "Cong nghe")]
    [InlineData("Học viện Công nghệ Bưu chính Viễn thông", "Bưu chính")]
    [InlineData("RMIT University (2016 - 2020)", "RMIT")]
    [InlineData("University of Science, VNU-HCM", "Science")]
    [InlineData("Cao đẳng FPT Polytechnic", "FPT Polytechnic")]
    public async Task Go_ten_truong(string line, string secret)
    {
        var result = await _anonymizer.AnonymizeAsync($"Học vấn:\n{line}\nKỹ năng: Java");

        Assert.DoesNotContain(secret, result.Text, StringComparison.Ordinal);
        Assert.Contains("[SCHOOL]", result.Text, StringComparison.Ordinal);
        Assert.Contains("Java", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Giu_chuyen_nganh_sau_ten_truong()
    {
        var result = await _anonymizer.AnonymizeAsync("Đại học Bách Khoa Hà Nội - chuyên ngành Khoa học máy tính");

        Assert.Contains("chuyên ngành Khoa học máy tính", result.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Ngày sinh: 12/03/1998")]
    [InlineData("Date of birth: 1998-03-12")]
    [InlineData("DOB - 12.03.1998")]
    public async Task Go_ngay_sinh(string line)
    {
        var result = await _anonymizer.AnonymizeAsync($"{line}\n5 năm kinh nghiệm");

        Assert.DoesNotContain("1998", result.Text, StringComparison.Ordinal);
        Assert.Contains("[DOB]", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Go_ngay_thang_day_du_khong_nhan()
    {
        var result = await _anonymizer.AnonymizeAsync("Sinh 05/11/1999 tại Huế");

        Assert.DoesNotContain("05/11/1999", result.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("NGUYỄN VĂN AN\nBackend Developer", "NGUYỄN VĂN AN")]
    [InlineData("Trần Thị Bình\n3 năm kinh nghiệm", "Trần Thị Bình")]
    [InlineData("Họ và tên: Lê Minh Châu\nKỹ năng: C#", "Lê Minh Châu")]
    [InlineData("Full name: Pham Duc Dung", "Pham Duc Dung")]
    public async Task Go_ho_ten(string text, string name)
    {
        var result = await _anonymizer.AnonymizeAsync(text);

        Assert.DoesNotContain(name, result.Text, StringComparison.Ordinal);
        Assert.Contains("[NAME]", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Khong_nuot_nam_lam_viec_hay_so_nam_kinh_nghiem()
    {
        var result = await _anonymizer.AnonymizeAsync(
            "Kinh nghiệm: 2019 - 2022 tại công ty phần mềm. Tổng 5 năm kinh nghiệm .NET, 03/2020 - 06/2023.");

        Assert.Contains("2019 - 2022", result.Text, StringComparison.Ordinal);
        Assert.Contains("03/2020 - 06/2023", result.Text, StringComparison.Ordinal);
        Assert.Contains("5 năm kinh nghiệm", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Trich_ky_nang_va_so_nam_kinh_nghiem()
    {
        var result = await _anonymizer.AnonymizeAsync(
            "Senior developer, 5+ năm kinh nghiệm với C#, ASP.NET Core, PostgreSQL, Docker, K8s và ReactJS. 2 years of Golang.");

        Assert.Contains("C#", result.Skills);
        Assert.Contains("ASP.NET Core", result.Skills);
        Assert.Contains("PostgreSQL", result.Skills);
        Assert.Contains("Docker", result.Skills);
        Assert.Contains("Kubernetes", result.Skills);
        Assert.Contains("React", result.Skills);
        Assert.Contains("Go", result.Skills);
        Assert.Contains("5 năm kinh nghiệm", result.ExperienceKeywords);
        Assert.Contains("2 năm kinh nghiệm", result.ExperienceKeywords);
        Assert.Contains("senior", result.ExperienceKeywords);
    }

    [Fact]
    public async Task Khong_khop_Java_trong_JavaScript()
    {
        var result = await _anonymizer.AnonymizeAsync("Thành thạo JavaScript và TypeScript");

        Assert.Contains("JavaScript", result.Skills);
        Assert.DoesNotContain("Java", result.Skills);
    }

    [Fact]
    public async Task CV_day_du_khong_con_PII_nao()
    {
        const string raw = """
            NGUYỄN VĂN AN
            Email: an.nguyen@example.com | SĐT: 0912 345 678
            Facebook: facebook.com/an.nguyen.92
            Địa chỉ: 12 Nguyễn Trãi, Quận 1, TP.HCM
            Ngày sinh: 12/03/1998
            Học vấn: Đại học Bách Khoa Hà Nội - chuyên ngành CNTT
            Kinh nghiệm: 4 năm kinh nghiệm C#, ASP.NET Core, PostgreSQL
            """;

        var result = await _anonymizer.AnonymizeAsync(raw);

        foreach (var secret in new[] { "NGUYỄN VĂN AN", "an.nguyen", "0912", "facebook.com", "Nguyễn Trãi", "1998", "Bách Khoa" })
        {
            Assert.DoesNotContain(secret, result.Text, StringComparison.Ordinal);
        }

        Assert.Contains("chuyên ngành CNTT", result.Text, StringComparison.Ordinal);
        Assert.Contains("C#", result.Skills);
        Assert.Contains("4 năm kinh nghiệm", result.ExperienceKeywords);
    }

    [Fact]
    public async Task Goi_IPiiRedactor_truoc_roi_regex_chay_tiep()
    {
        var anonymizer = new SimpleAnonymizer(new UpperRedactor());

        var result = await anonymizer.AnonymizeAsync("ten la Bao, email bao@x.com");

        Assert.StartsWith("TEN LA", result.Text, StringComparison.Ordinal);
        Assert.Contains("[EMAIL]", result.Text, StringComparison.Ordinal);
    }

    private sealed class UpperRedactor : IPiiRedactor
    {
        public Task<string> RedactAsync(string rawCvText, CancellationToken ct = default)
            => Task.FromResult(rawCvText.ToUpperInvariant());
    }
}
