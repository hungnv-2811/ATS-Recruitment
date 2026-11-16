using System.Text;
using ATS.Recruitment.Infrastructure.Shared;
using Xunit;

namespace ATS.IntegrationTests.Shared;

/// <summary>LocalFileStorage (TV1): ten file do he thong sinh, khong doc/xoa duoc ngoai root.</summary>
public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _sandbox = Path.Combine(Path.GetTempPath(), "ats-fs-" + Guid.NewGuid().ToString("N"));
    private readonly string _root;
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _root = Path.Combine(_sandbox, "cvs");
        _storage = new LocalFileStorage(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sandbox))
        {
            Directory.Delete(_sandbox, recursive: true);
        }
    }

    private static MemoryStream Content(string text) => new(Encoding.UTF8.GetBytes(text));

    [Fact]
    public async Task Luu_roi_doc_lai_va_xoa_duoc()
    {
        var path = await _storage.SaveAsync(Content("noi dung CV"), "CV Nguyen Van A.PDF");

        Assert.EndsWith(".pdf", path, StringComparison.Ordinal);
        Assert.DoesNotContain("Nguyen", path, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(_root, path)));

        var stream = await _storage.ReadAsync(path);
        using (var reader = new StreamReader(stream))
        {
            Assert.Equal("noi dung CV", await reader.ReadToEndAsync());
        }

        await _storage.DeleteAsync(path);
        Assert.False(File.Exists(Path.Combine(_root, path)));
    }

    [Theory]
    [InlineData("../../evil.pdf")]
    [InlineData("..\\..\\evil.pdf")]
    [InlineData("/etc/passwd")]
    public async Task Ten_file_goc_doc_hai_khong_thoat_ra_ngoai_root(string fileName)
    {
        var path = await _storage.SaveAsync(Content("x"), fileName);

        Assert.DoesNotContain("..", path, StringComparison.Ordinal);
        Assert.DoesNotContain('/', path);
        Assert.DoesNotContain('\\', path);
        Assert.Single(Directory.GetFiles(_root));
    }

    [Theory]
    [InlineData("cv.exe;rm -rf", "")]
    [InlineData("cv", "")]
    [InlineData("cv.docx", ".docx")]
    public async Task Duoi_file_chi_giu_ky_tu_an_toan(string fileName, string expectedExtension)
    {
        var path = await _storage.SaveAsync(Content("x"), fileName);

        Assert.Equal(expectedExtension, Path.GetExtension(path));
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("../cvs-khac/secret.txt")]
    [InlineData("sub/../../secret.txt")]
    [InlineData("..")]
    [InlineData(".")]
    public async Task Doc_va_xoa_ngoai_root_bi_chan(string path)
    {
        // File "bi mat" nam canh root — neu path traversal lot qua thi doc/xoa duoc no.
        Directory.CreateDirectory(Path.Combine(_sandbox, "cvs-khac"));
        await File.WriteAllTextAsync(Path.Combine(_sandbox, "secret.txt"), "bi mat");
        await File.WriteAllTextAsync(Path.Combine(_sandbox, "cvs-khac", "secret.txt"), "bi mat");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _storage.ReadAsync(path));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _storage.DeleteAsync(path));

        Assert.True(File.Exists(Path.Combine(_sandbox, "secret.txt")));
        Assert.True(File.Exists(Path.Combine(_sandbox, "cvs-khac", "secret.txt")));
    }

    [Fact]
    public async Task Duong_dan_tuyet_doi_bi_chan_ke_ca_khi_nam_trong_root()
    {
        var path = await _storage.SaveAsync(Content("x"), "a.pdf");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _storage.ReadAsync(Path.Combine(_root, path)));
    }

    [Fact]
    public async Task Xoa_file_khong_ton_tai_khong_loi()
    {
        await _storage.DeleteAsync("khong-co.pdf");
    }
}
