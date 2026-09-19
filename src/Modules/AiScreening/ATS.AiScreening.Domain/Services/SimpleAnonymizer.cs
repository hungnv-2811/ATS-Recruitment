using System.Text.RegularExpressions;
using ATS.AiScreening.Domain.Ports;

namespace ATS.AiScreening.Domain.Services;

/// <summary>
/// An danh CV bang regex. Regex thuan, khong I/O, khong SDK ngoai — nen dat trong
/// Domain hoan toan hop le, va DAY LA DIEU KIEN de constructor <c>internal</c> cua
/// <see cref="AnonymizedCv"/> hoat dong ma khong can <c>InternalsVisibleTo</c>.
/// </summary>
/// <remarks>
/// Phase 2: truyen them <see cref="IPiiRedactor"/> (LLM, ben Infrastructure) de chay
/// truoc, regex o day van chay tiep nhu luoi an toan thu hai.
/// TODO tuan 5: mo rong bo regex va do ti le sot tren 20 CV that.
/// </remarks>
public sealed partial class SimpleAnonymizer : IAnonymizer
{
    private readonly IPiiRedactor? _redactor;

    public SimpleAnonymizer(IPiiRedactor? redactor = null) => _redactor = redactor;

    public async Task<AnonymizedCv> AnonymizeAsync(string rawCvText, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawCvText);

        // Tang 1 (tuy chon, phase 2): LLM go PII tinh vi hon.
        var text = _redactor is null
            ? rawCvText
            : await _redactor.RedactAsync(rawCvText, ct).ConfigureAwait(false);

        // Tang 2 (luon chay): luoi an toan bang regex.
        text = EmailRegex().Replace(text, "[EMAIL]");
        text = PhoneRegex().Replace(text, "[PHONE]");
        text = AddressRegex().Replace(text, "[ADDRESS]");
        text = SchoolRegex().Replace(text, "[SCHOOL]");

        return new AnonymizedCv(text, skills: [], experienceKeywords: []);
    }

    [GeneratedRegex(@"[\w\.\-\+]+@[\w\-]+(\.[\w\-]+)+", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(\+?84|0)\d{9,10}")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"(?i)(dia\s*chi|address)\s*:\s*.+")]
    private static partial Regex AddressRegex();

    [GeneratedRegex(@"(?i)(truong\s+)?(dai\s*hoc|cao\s*dang|university|college)\s+[^\r\n,;]{0,60}")]
    private static partial Regex SchoolRegex();
}
