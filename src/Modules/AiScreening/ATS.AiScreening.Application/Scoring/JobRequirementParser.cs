using System.Globalization;
using System.Text.RegularExpressions;
using ATS.AiScreening.Application.Ports;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Services;

namespace ATS.AiScreening.Application.Scoring;

/// <summary>
/// Bien tin tuyen dung (van ban tu do cua HR) thanh <see cref="JobRequirement"/> co cau truc.
/// </summary>
/// <remarks>
/// Quy tac:
/// <list type="bullet">
/// <item>Tach <c>Requirements</c> theo dong, dau phay, cham phay, gach dau dong.</item>
/// <item>Manh co ky nang trong <see cref="SkillCatalog"/> → lay TEN CHUAN cua ky nang do.</item>
/// <item>Manh ngan (≤ 5 tu) khong co trong danh muc → giu nguyen lam mot ky nang tu do.</item>
/// <item>Cau dai khong chua ky nang nao → bo qua (la mo ta, khong phai ky nang).</item>
/// <item>Dong/muc co "ưu tiên", "nice to have", "lợi thế", "plus"... → <c>NiceToHaveSkills</c>.</item>
/// <item><c>MinYearsExperience</c> = so lon nhat trong cac cum "N năm"/"N years".</item>
/// </list>
/// </remarks>
public static partial class JobRequirementParser
{
    private const int MaxFreeSkillWords = 5;
    private const int MaxFreeSkillLength = 40;

    public static JobRequirement Parse(JobInfo job)
    {
        ArgumentNullException.ThrowIfNull(job);

        var required = new List<string>();
        var nice = new List<string>();
        var inNiceSection = false;

        foreach (var rawLine in (job.Requirements ?? string.Empty).Split('\n'))
        {
            var line = BulletRegex().Replace(rawLine.Trim(), string.Empty).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var isNiceLine = NiceMarkerRegex().IsMatch(line);
            var headerOnly = line.EndsWith(':');
            if (headerOnly)
            {
                inNiceSection = isNiceLine;
                continue;
            }

            if (RequiredHeaderRegex().IsMatch(line) && !isNiceLine)
            {
                inNiceSection = false;
            }

            var target = isNiceLine || inNiceSection ? nice : required;
            target.AddRange(SkillsOfLine(line));
        }

        var requiredDistinct = required.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var niceDistinct = nice
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(s => !requiredDistinct.Contains(s, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        var minYears = SkillCatalog.ExtractMaxYears(job.Requirements ?? string.Empty);
        if (minYears == 0)
        {
            minYears = SkillCatalog.ExtractMaxYears(job.Description ?? string.Empty);
        }

        return new JobRequirement(
            job.Title,
            job.Description ?? string.Empty,
            requiredDistinct,
            niceDistinct,
            minYears);
    }

    /// <summary>Van ban dai dien JD trong CacheKey: doi bat ky yeu cau nao thi khoa doi theo.</summary>
    public static string ToCacheText(JobRequirement jd)
    {
        ArgumentNullException.ThrowIfNull(jd);

        return string.Join(
            "\n",
            jd.Title,
            jd.Description,
            string.Join(",", jd.RequiredSkills),
            string.Join(",", jd.NiceToHaveSkills),
            jd.MinYearsExperience.ToString(CultureInfo.InvariantCulture));
    }

    private static IEnumerable<string> SkillsOfLine(string line)
    {
        // "Kỹ năng: C#, SQL" → bo phan nhan truoc dau hai cham.
        var colon = line.IndexOf(':', StringComparison.Ordinal);
        if (colon >= 0 && colon < line.Length - 1)
        {
            line = line[(colon + 1)..];
        }

        foreach (var piece in SeparatorRegex().Split(line))
        {
            var fragment = piece.Trim().TrimEnd('.', '!', ';').Trim();
            if (fragment.Length == 0)
            {
                continue;
            }

            var known = SkillCatalog.ExtractKnownSkills(fragment);
            if (known.Count > 0)
            {
                foreach (var skill in known)
                {
                    yield return skill;
                }

                continue;
            }

            if (IsFreeSkill(fragment))
            {
                yield return fragment;
            }
        }
    }

    private static bool IsFreeSkill(string fragment)
    {
        var words = fragment.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= MaxFreeSkillWords
               && fragment.Length <= MaxFreeSkillLength
               && fragment.Any(char.IsLetter)
               && SkillCatalog.ExtractMaxYears(fragment) == 0
               && !NiceMarkerRegex().IsMatch(fragment);
    }

    // "- ", "* ", "• ", "1. ", "2) " — so thu tu chi tinh khi co dau cham/ngoac, de "3 năm" khong mat so.
    [GeneratedRegex(@"^(?:[\s\-*•+·–—>]|\d{1,2}[.)](?=\s))+")]
    private static partial Regex BulletRegex();

    [GeneratedRegex(@"[,;|]|\s+(?:và|va|and|hoặc|hoac|or)\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SeparatorRegex();

    [GeneratedRegex(@"ưu tiên|uu tien|nice to have|nice-to-have|lợi thế|loi the|điểm cộng|diem cong|\bplus\b|\bbonus\b|preferred", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NiceMarkerRegex();

    [GeneratedRegex(@"^(?:yêu cầu|yeu cau|requirements?|bắt buộc|bat buoc|must have)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RequiredHeaderRegex();
}
