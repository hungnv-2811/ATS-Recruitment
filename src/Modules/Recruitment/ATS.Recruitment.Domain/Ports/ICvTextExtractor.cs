namespace ATS.Recruitment.Domain.Ports;

/// <summary>Trich van ban tu file CV (PDF/DOCX). Adapter: PdfPig, OpenXml — TV1.</summary>
/// <remarks>
/// Khong doc duoc (CV thiet ke la, file scan) thi tra chuoi rong, KHONG nem loi:
/// ung vien van duoc apply, chi la AI se cham thap / Failed (weekly-plan, bang rui ro).
/// </remarks>
public interface ICvTextExtractor
{
    /// <param name="content">Noi dung file.</param>
    /// <param name="fileType">"pdf" | "docx" (chu thuong, khong dau cham).</param>
    /// <param name="ct">Token huy.</param>
    Task<string> ExtractAsync(Stream content, string fileType, CancellationToken ct = default);
}
