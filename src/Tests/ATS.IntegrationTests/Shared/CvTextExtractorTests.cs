using System.Text;
using ATS.Recruitment.Domain.Ports;
using ATS.Recruitment.Infrastructure.Shared;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging.Abstractions;
using PdfPageSize = UglyToad.PdfPig.Content.PageSize;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace ATS.IntegrationTests.Shared;

/// <summary>CompositeCvTextExtractor (TV1): chon theo fileType, file hong → "" chu khong nem loi.</summary>
public sealed class CvTextExtractorTests
{
    private static readonly ICvTextExtractor Extractor = new CompositeCvTextExtractor(
        [new PdfPigTextExtractor(), new OpenXmlTextExtractor()],
        NullLogger<CompositeCvTextExtractor>.Instance);

    private static MemoryStream Docx(params string[] paragraphs)
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(
                paragraphs.Select(p => new Paragraph(new Run(new Text(p)))).ToArray<OpenXmlElement>()));
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream Pdf(string text)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(PdfPageSize.A4);
        page.AddText(text, 12, new PdfPoint(25, 700), font);
        return new MemoryStream(builder.Build());
    }

    [Fact]
    public async Task Docx_trich_duoc_moi_doan_van()
    {
        var text = await Extractor.ExtractAsync(Docx("Kinh nghiem 3 nam C#", "ASP.NET Core, PostgreSQL"), "docx");

        Assert.Contains("Kinh nghiem 3 nam C#", text, StringComparison.Ordinal);
        Assert.Contains("ASP.NET Core, PostgreSQL", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pdf_trich_duoc_text()
    {
        var text = await Extractor.ExtractAsync(Pdf("Backend Developer"), "pdf");

        Assert.Contains("Backend", text, StringComparison.Ordinal);
        Assert.Contains("Developer", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("PDF")]
    [InlineData(".docx")]
    [InlineData(" Docx ")]
    public async Task FileType_khong_phan_biet_hoa_thuong_va_dau_cham(string fileType)
    {
        var content = fileType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
            ? Pdf("Hello CV")
            : Docx("Hello CV");

        var text = await Extractor.ExtractAsync(content, fileType);

        Assert.Contains("Hello", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("pdf")]
    [InlineData("docx")]
    public async Task File_hong_tra_chuoi_rong_khong_nem_loi(string fileType)
    {
        var garbage = new MemoryStream(Encoding.UTF8.GetBytes("day khong phai file " + fileType));

        var text = await Extractor.ExtractAsync(garbage, fileType);

        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public async Task File_rong_tra_chuoi_rong()
    {
        Assert.Equal(string.Empty, await Extractor.ExtractAsync(new MemoryStream(), "pdf"));
        Assert.Equal(string.Empty, await Extractor.ExtractAsync(new MemoryStream(), "docx"));
    }

    [Fact]
    public async Task Kieu_file_khong_ho_tro_tra_chuoi_rong()
    {
        var text = await Extractor.ExtractAsync(new MemoryStream([1, 2, 3]), "png");

        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public async Task Bi_huy_thi_nem_OperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Extractor.ExtractAsync(Docx("x"), "docx", cts.Token));
    }
}
