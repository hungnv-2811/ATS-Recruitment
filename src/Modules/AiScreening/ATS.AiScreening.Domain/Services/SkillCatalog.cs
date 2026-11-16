using System.Globalization;
using System.Text.RegularExpressions;

namespace ATS.AiScreening.Domain.Services;

/// <summary>
/// Danh sach ky nang pho bien + cach so khop "nguyen tu" trong van ban. Logic thuan, khong I/O.
/// </summary>
/// <remarks>
/// Dung o ba cho: <see cref="SimpleAnonymizer"/> trich <c>AnonymizedCv.Skills</c>, phan tich JD
/// thanh <c>RequiredSkills</c>, va <c>KeywordScoringAdapter</c> dem ky nang khop. Chung MOT bo so
/// khop nen ba cho khong bao gio lech nhau (vd "C#" khop o CV nhung khong khop o JD).
/// </remarks>
public static partial class SkillCatalog
{
    private sealed record SkillEntry(string Name, string[] Aliases, bool CaseSensitive = false);

    private static readonly SkillEntry[] Entries =
    [
        new("C#", ["C#", "CSharp", "C Sharp"]),
        new(".NET", [".NET", "dotnet", ".NET Core", ".NET Framework"]),
        new("ASP.NET Core", ["ASP.NET Core", "ASP.NET", "ASP.NET MVC", "Web API"]),
        new("Entity Framework", ["Entity Framework", "EF Core"]),
        new("LINQ", ["LINQ"]),
        new("Blazor", ["Blazor"]),
        new("Java", ["Java"]),
        new("Spring Boot", ["Spring Boot", "Spring"]),
        new("Kotlin", ["Kotlin"]),
        new("Python", ["Python"]),
        new("Django", ["Django"]),
        new("Flask", ["Flask"]),
        new("FastAPI", ["FastAPI"]),
        new("JavaScript", ["JavaScript", "ECMAScript", "ES6"]),
        new("TypeScript", ["TypeScript"]),
        new("Node.js", ["Node.js", "NodeJS", "Node"]),
        new("React", ["React", "ReactJS", "React.js"]),
        new("React Native", ["React Native"]),
        new("Angular", ["Angular"]),
        new("Vue.js", ["Vue.js", "VueJS", "Vue"]),
        new("Next.js", ["Next.js", "NextJS"]),
        new("HTML", ["HTML", "HTML5"]),
        new("CSS", ["CSS", "CSS3", "SCSS", "SASS"]),
        new("Tailwind CSS", ["Tailwind CSS", "Tailwind"]),
        new("PHP", ["PHP"]),
        new("Laravel", ["Laravel"]),
        new("Go", ["Golang", "Go"], CaseSensitive: true),
        new("Rust", ["Rust"]),
        new("Ruby", ["Ruby", "Ruby on Rails", "Rails"]),
        new("Swift", ["Swift"]),
        new("Flutter", ["Flutter", "Dart"]),
        new("Android", ["Android"]),
        new("iOS", ["iOS"]),
        new("SQL", ["SQL", "T-SQL", "PL/SQL"]),
        new("PostgreSQL", ["PostgreSQL", "Postgres"]),
        new("MySQL", ["MySQL", "MariaDB"]),
        new("SQL Server", ["SQL Server", "MSSQL"]),
        new("Oracle", ["Oracle"]),
        new("MongoDB", ["MongoDB", "Mongo"]),
        new("Redis", ["Redis"]),
        new("Elasticsearch", ["Elasticsearch", "ElasticSearch", "ELK"]),
        new("RabbitMQ", ["RabbitMQ"]),
        new("Kafka", ["Kafka"]),
        new("Docker", ["Docker"]),
        new("Kubernetes", ["Kubernetes", "K8s"]),
        new("AWS", ["AWS", "Amazon Web Services"]),
        new("Azure", ["Azure"]),
        new("GCP", ["GCP", "Google Cloud"]),
        new("Linux", ["Linux", "Ubuntu"]),
        new("Git", ["Git", "GitHub", "GitLab"]),
        new("CI/CD", ["CI/CD", "GitHub Actions", "Jenkins", "GitLab CI"]),
        new("Terraform", ["Terraform"]),
        new("REST API", ["REST API", "RESTful", "REST"]),
        new("GraphQL", ["GraphQL"]),
        new("gRPC", ["gRPC"]),
        new("Microservices", ["Microservices", "Microservice"]),
        new("Clean Architecture", ["Clean Architecture", "DDD", "Domain-Driven Design"]),
        new("Unit Testing", ["Unit Testing", "Unit Test", "xUnit", "NUnit", "JUnit", "Jest", "TDD"]),
        new("Selenium", ["Selenium", "Playwright", "Cypress"]),
        new("Machine Learning", ["Machine Learning", "ML"]),
        new("Deep Learning", ["Deep Learning"]),
        new("TensorFlow", ["TensorFlow"]),
        new("PyTorch", ["PyTorch"]),
        new("Pandas", ["Pandas"]),
        new("NumPy", ["NumPy"]),
        new("Power BI", ["Power BI", "PowerBI"]),
        new("Tableau", ["Tableau"]),
        new("Excel", ["Excel"]),
        new("Figma", ["Figma"]),
        new("Agile", ["Agile", "Scrum", "Kanban"]),
        new("Jira", ["Jira"]),
        new("Tieng Anh", ["IELTS", "TOEIC", "TOEFL", "English", "tiếng Anh", "tieng Anh"]),
    ];

    /// <summary>Ten chuan cua moi ky nang trong danh muc.</summary>
    public static IReadOnlyList<string> KnownSkills { get; } = Entries.Select(e => e.Name).ToArray();

    /// <summary>Moi ky nang trong danh muc xuat hien trong <paramref name="text"/>, theo ten chuan, khong trung.</summary>
    public static IReadOnlyList<string> ExtractKnownSkills(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return Entries
            .Where(e => e.Aliases.Any(alias => ContainsTerm(text, alias, e.CaseSensitive)))
            .Select(e => e.Name)
            .ToArray();
    }

    /// <summary>
    /// Ky nang <paramref name="skill"/> (ten chuan hoac ten tu do lay tu JD) co xuat hien trong
    /// <paramref name="text"/> khong. Ky nang co trong danh muc thi so ca ten goi khac (alias).
    /// </summary>
    public static bool Mentions(string text, string skill)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(skill);

        var entry = Find(skill);
        return entry is null
            ? ContainsTerm(text, skill.Trim(), caseSensitive: false)
            : entry.Aliases.Append(entry.Name).Any(alias => ContainsTerm(text, alias, entry.CaseSensitive));
    }

    /// <summary>Ten chuan neu <paramref name="skill"/> la ten/alias co trong danh muc; nguoc lai tra lai chinh no (da trim).</summary>
    public static string Canonicalize(string skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        return Find(skill)?.Name ?? skill.Trim();
    }

    /// <summary>So khop "nguyen tu": khong khop "Java" trong "JavaScript", khong khop ".NET" trong "ASP.NET".</summary>
    public static bool ContainsTerm(string text, string term, bool caseSensitive)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var start = 0;
        while (start <= text.Length - term.Length)
        {
            var index = text.IndexOf(term, start, comparison);
            if (index < 0)
            {
                return false;
            }

            var end = index + term.Length;
            var beforeOk = index == 0 || !IsWordChar(text[index - 1]);
            var afterOk = end >= text.Length || !(IsWordChar(text[end]) || text[end] is '#' or '+');
            if (beforeOk && afterOk)
            {
                return true;
            }

            start = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Trich "N nam kinh nghiem" (va tu khoa cap bac: senior, junior...) tu van ban.
    /// Ket qua da chuan hoa, vd <c>"5 năm kinh nghiệm"</c>, <c>"senior"</c>.
    /// </summary>
    public static IReadOnlyList<string> ExtractExperienceKeywords(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var result = new List<string>();
        foreach (Match match in YearsRegex().Matches(text))
        {
            var years = int.Parse(match.Groups["n"].Value, CultureInfo.InvariantCulture);
            if (years is > 0 and <= 40)
            {
                result.Add(string.Create(CultureInfo.InvariantCulture, $"{years} năm kinh nghiệm"));
            }
        }

        foreach (Match match in SeniorityRegex().Matches(text))
        {
            result.Add(match.Value.ToLowerInvariant());
        }

        return result.Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>So nam kinh nghiem lon nhat nhac toi trong van ban (0 neu khong co).</summary>
    public static int ExtractMaxYears(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var max = 0;
        foreach (Match match in YearsRegex().Matches(text))
        {
            var years = int.Parse(match.Groups["n"].Value, CultureInfo.InvariantCulture);
            if (years is > 0 and <= 40)
            {
                max = Math.Max(max, years);
            }
        }

        return max;
    }

    private static SkillEntry? Find(string skill)
    {
        var trimmed = skill.Trim();
        return Entries.FirstOrDefault(e =>
            string.Equals(e.Name, trimmed, StringComparison.OrdinalIgnoreCase)
            || e.Aliases.Any(a => string.Equals(a, trimmed, e.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    // "5 năm", "5+ năm kinh nghiệm", "3 nam", "4 years", "2 yrs". So nam phai dung truoc don vi.
    [GeneratedRegex(@"(?<![\d.,])(?<n>\d{1,2})\s*\+?\s*(?:năm|nam|years?|yrs?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex YearsRegex();

    [GeneratedRegex(@"\b(?:senior|junior|fresher|intern|middle|mid-level|lead|team lead|tech lead|principal|manager)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SeniorityRegex();
}
