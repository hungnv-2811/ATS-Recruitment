using System.Text;
using ATS.Recruitment.Domain.Ports;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;

namespace ATS.Recruitment.Infrastructure.Shared;

/// <summary>Trich text tu MOT dinh dang file. Duoc phep nem loi — <see cref="CompositeCvTextExtractor"/> bat.</summary>
public interface ICvFormatTextExtractor
{
    /// <summary>Kieu file xu ly duoc: "pdf" | "docx" (chu thuong, khong dau cham).</summary>
    string FileType { get; }

    Task<string> ExtractAsync(Stream content, CancellationToken ct = default);
}

/// <summary>PDF → text bang PdfPig (thuan .NET, khong can thu vien native).</summary>
/// <remarks>PDF scan (chi co anh) cho ra chuoi rong — khong OCR o phase 1.</remarks>
public sealed class PdfPigTextExtractor : ICvFormatTextExtractor
{
    public string FileType => "pdf";

    public async Task<string> ExtractAsync(Stream content, CancellationToken ct = default)
    {
        var seekable = await CvStreams.ToSeekableAsync(content, ct).ConfigureAwait(false);
        await using (seekable.ConfigureAwait(false))
        {
            using var document = PdfDocument.Open(seekable);
            var text = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                ct.ThrowIfCancellationRequested();
                text.AppendLine(ContentOrderTextExtractor.GetText(page));
            }

            return text.ToString();
        }
    }
}

/// <summary>DOCX → text bang DocumentFormat.OpenXml (moi doan van mot dong, ke ca trong bang).</summary>
public sealed class OpenXmlTextExtractor : ICvFormatTextExtractor
{
    public string FileType => "docx";

    public async Task<string> ExtractAsync(Stream content, CancellationToken ct = default)
    {
        var seekable = await CvStreams.ToSeekableAsync(content, ct).ConfigureAwait(false);
        await using (seekable.ConfigureAwait(false))
        {
            using var document = WordprocessingDocument.Open(seekable, isEditable: false);
            var body = document.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                return string.Empty;
            }

            var text = new StringBuilder();
            foreach (var paragraph in body.Descendants<WordParagraph>())
            {
                ct.ThrowIfCancellationRequested();
                text.AppendLine(paragraph.InnerText);
            }

            return text.ToString();
        }
    }
}

/// <summary>
/// <see cref="ICvTextExtractor"/> duy nhat dang ky trong DI: chon extractor theo <c>fileType</c>.
/// </summary>
/// <remarks>
/// Hop dong cua port: KHONG nem loi. File hong, dinh dang la, PDF scan → tra <c>""</c> va ghi
/// warning; ung vien van upload/apply duoc (CvDto.HasText=false). Chi nem lai khi bi huy.
/// Ky tu NUL bi loai vi cot <c>text</c> cua PostgreSQL khong chap nhan.
/// </remarks>
public sealed class CompositeCvTextExtractor : ICvTextExtractor
{
    private readonly Dictionary<string, ICvFormatTextExtractor> _byType;
    private readonly ILogger<CompositeCvTextExtractor> _logger;

    public CompositeCvTextExtractor(
        IEnumerable<ICvFormatTextExtractor> extractors,
        ILogger<CompositeCvTextExtractor> logger)
    {
        ArgumentNullException.ThrowIfNull(extractors);
        _byType = extractors.ToDictionary(e => e.FileType, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task<string> ExtractAsync(Stream content, string fileType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var normalized = (fileType ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        if (!_byType.TryGetValue(normalized, out var extractor))
        {
            _logger.LogWarning("Khong ho tro trich text cho kieu file \"{FileType}\" — tra chuoi rong.", normalized);
            return string.Empty;
        }

        try
        {
            var text = await extractor.ExtractAsync(content, ct).ConfigureAwait(false);
            return text.Replace("\0", string.Empty, StringComparison.Ordinal).Trim();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Khong ghi noi dung file (PII) — chi kieu file va loai loi.
            _logger.LogWarning(
                ex,
                "Khong trich duoc text tu file {FileType} ({ErrorType}) — tra chuoi rong.",
                normalized, ex.GetType().Name);
            return string.Empty;
        }
    }
}

internal static class CvStreams
{
    /// <summary>PdfPig va OpenXml can stream seek duoc; stream upload thuong khong seek duoc.</summary>
    /// <remarks>Luon tra stream MOI (copy) de dispose khong dong stream cua nguoi goi.</remarks>
    public static async Task<Stream> ToSeekableAsync(Stream content, CancellationToken ct)
    {
        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct).ConfigureAwait(false);
        buffer.Position = 0;
        return buffer;
    }
}
