namespace ATS.AiScreening.Domain;

/// <summary>
/// CV DA duoc an danh — kieu du lieu rieng, khong phai string.
/// </summary>
/// <remarks>
/// Constructor la <c>internal</c> nen CHI code trong chinh assembly
/// <c>ATS.AiScreening.Domain</c> tao duoc. <see cref="Services.SimpleAnonymizer"/>
/// nam ngay trong assembly nay, nho vay khong can <c>InternalsVisibleTo</c> cho
/// bat ky assembly nao khac.
///
/// TUYET DOI KHONG them InternalsVisibleTo: lam vay la vut bo bao dam
/// compile-time cho RB3 ma ca do an dua vao. Anonymizer bang LLM phai di qua
/// <see cref="Ports.IPiiRedactor"/> (chi nhan/tra string).
///
/// Xem docs/architecture.md ADR-3.
/// </remarks>
public sealed record AnonymizedCv
{
    internal AnonymizedCv(
        string text,
        IEnumerable<string> skills,
        IEnumerable<string> experienceKeywords)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Text = text;
        Skills = skills.ToArray();
        ExperienceKeywords = experienceKeywords.ToArray();
    }

    /// <summary>Noi dung CV da go bo PII. Day la thu DUY NHAT duoc gui len LLM.</summary>
    public string Text { get; }

    public IReadOnlyList<string> Skills { get; }

    public IReadOnlyList<string> ExperienceKeywords { get; }
}

