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
///
/// Thu tu thay the CO Y NGHIA: link mang xa hoi truoc email (tranh "linkedin.com/in/a@b"),
/// ngay sinh truoc ngay thang tran, dong "Ho ten:" truoc heuristic dong dau.
/// Ky nang va "N nam kinh nghiem" trich tren van ban DA go PII — chung khong phai PII
/// va la thu cac adapter cham diem can.
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
        text = Redact(text);

        return new AnonymizedCv(
            text,
            skills: SkillCatalog.ExtractKnownSkills(text),
            experienceKeywords: SkillCatalog.ExtractExperienceKeywords(text));
    }

    /// <summary>Chi phan go PII — tach ra de test va de doc de hon.</summary>
    private static string Redact(string text)
    {
        text = SocialLinkRegex().Replace(text, "[LINK]");
        text = EmailRegex().Replace(text, "[EMAIL]");
        text = NameLabelRegex().Replace(text, m => m.Groups["label"].Value + "[NAME]");
        text = BirthDateLabelRegex().Replace(text, m => m.Groups["label"].Value + "[DOB]");
        text = FullDateRegex().Replace(text, "[DATE]");
        text = PhoneRegex().Replace(text, "[PHONE]");
        text = AddressLabelRegex().Replace(text, m => m.Groups["label"].Value + "[ADDRESS]");
        text = StreetAddressRegex().Replace(text, "[ADDRESS]");
        text = SchoolSuffixRegex().Replace(text, "[SCHOOL]");
        text = SchoolRegex().Replace(text, "[SCHOOL]");
        text = RedactLeadingName(text);
        return text;
    }

    /// <summary>
    /// Dong khong rong DAU TIEN gom 2–5 tu viet hoa chu cai dau, khong so, khong dau hai cham —
    /// gan nhu chac chan la ho ten ("NGUYỄN VĂN AN", "Trần Thị Bình").
    /// </summary>
    private static string RedactLeadingName(string text)
    {
        var match = FirstLineRegex().Match(text);
        if (!match.Success)
        {
            return text;
        }

        var line = match.Groups["line"].Value.Trim();
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var looksLikeName = words.Length is >= 2 and <= 5
                            && words.All(w => char.IsUpper(w[0]) && w.All(char.IsLetter))
                            && SkillCatalog.ExtractKnownSkills(line).Count == 0;

        return looksLikeName
            ? string.Concat(text.AsSpan(0, match.Groups["line"].Index), "[NAME]", text.AsSpan(match.Groups["line"].Index + match.Groups["line"].Length))
            : text;
    }

    // facebook.com/x, fb.com/x, m.facebook.com/x, linkedin.com/in/x, github.com/x, co hoac khong http(s)://www.
    [GeneratedRegex(@"(?:https?://)?(?:[\w-]+\.)?(?:facebook|fb|linkedin|github|zalo)\.(?:com|me)/[^\s,;)\]]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SocialLinkRegex();

    [GeneratedRegex(@"[\w\.\-\+]+@[\w\-]+(\.[\w\-]+)+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    // SDT VN: 0912345678, 0912 345 678, 091.234.5678, 091-234-5678, +84 912 345 678, (+84) 912345678, 84912345678.
    // Can 9–10 chu so sau dau so: nam "2019 - 2021" hay "5 năm" khong bi nuot.
    [GeneratedRegex(@"(?<![\w+])(?:\(?\+?84\)?[\s.\-]?|0)\d(?:[\s.\-]?\d){7,9}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"(?im)(?<label>\b(?:họ\s*(?:và\s*)?tên|ho\s*(?:va\s*)?ten|full\s*name|name|tên|ten)\s*[:\-]\s*)[^\r\n]+")]
    private static partial Regex NameLabelRegex();

    [GeneratedRegex(@"(?im)(?<label>\b(?:ngày\s*sinh|ngay\s*sinh|năm\s*sinh|nam\s*sinh|sinh\s*ngày|sinh\s*ngay|date\s*of\s*birth|dob|birthday|birth\s*date)\s*[:\-]?\s*)[^\r\n]+")]
    private static partial Regex BirthDateLabelRegex();

    // Ngay du 3 phan (dd/mm/yyyy) hau nhu chi la ngay sinh; moc lam viec trong CV thuong la mm/yyyy.
    [GeneratedRegex(@"\b\d{1,2}[/\-.]\d{1,2}[/\-.](?:19|20)\d{2}\b", RegexOptions.CultureInvariant)]
    private static partial Regex FullDateRegex();

    [GeneratedRegex(@"(?im)(?<label>\b(?:địa\s*chỉ|dia\s*chi|address|nơi\s*ở|noi\s*o|chỗ\s*ở|cho\s*o|thường\s*trú|thuong\s*tru|quê\s*quán|que\s*quan|hometown)\s*[:\-]\s*)[^\r\n]+")]
    private static partial Regex AddressLabelRegex();

    // "Số 12 ngõ 34 đường Láng", "123 Nguyễn Trãi, Quận 1" ... — so nha + tu khoa duong/pho/ngo/hem.
    [GeneratedRegex(@"(?i)\b(?:số\s*|so\s*)?\d+[A-Za-z]?(?:/\d+)*\s+(?:đường|duong|phố|pho|ngõ|ngo|ngách|ngach|hẻm|hem|kiệt|kiet)\s+[^\r\n]+")]
    private static partial Regex StreetAddressRegex();

    [GeneratedRegex(@"(?i)(?:trường\s+|truong\s+)?(?:đại\s*học|dai\s*hoc|cao\s*đẳng|cao\s*dang|học\s*viện|hoc\s*vien|THPT|trung\s*học|trung\s*hoc|university|college|academy|institute\s+of)[ \t]+[^\r\n,;\-–(]{0,60}")]
    private static partial Regex SchoolRegex();

    // "RMIT University", "FPT University" — ten truong dung TRUOC tu khoa.
    [GeneratedRegex(@"\b(?:[A-Z][\w&]*[ \t]+){1,4}(?:University|College|Academy)\b(?![ \t]+of\b)", RegexOptions.CultureInvariant)]
    private static partial Regex SchoolSuffixRegex();

    [GeneratedRegex(@"\A\s*(?<line>[^\r\n:]+)")]
    private static partial Regex FirstLineRegex();
}
