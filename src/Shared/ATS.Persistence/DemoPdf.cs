using System.Globalization;
using System.Text;

namespace ATS.Persistence;

/// <summary>
/// Sinh PDF mot trang, chu Helvetica, tu van ban ASCII — du de LocalFileStorage tra file that va
/// PdfPig doc lai duoc. Khong keo them thu vien PDF vao ATS.Persistence chi vi du lieu demo.
/// </summary>
internal static class DemoPdf
{
    public static byte[] Create(string text)
    {
        var content = new StringBuilder("BT /F1 11 Tf 14 TL 50 790 Td\n");
        foreach (var line in text.Split('\n'))
        {
            content.Append('(').Append(Escape(line.TrimEnd('\r'))).Append(") Tj T*\n");
        }

        content.Append("ET");
        var stream = content.ToString();

        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream",
        ];

        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xref = pdf.Length;
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");

        // Chi ASCII nen so ky tu = so byte, offset o tren dung.
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string Escape(string line)
    {
        var sb = new StringBuilder(line.Length);
        foreach (var ch in line)
        {
            if (ch is '(' or ')' or '\\')
            {
                sb.Append('\\');
            }

            sb.Append(ch is >= ' ' and <= '~' ? ch : '?');
        }

        return sb.ToString();
    }
}
