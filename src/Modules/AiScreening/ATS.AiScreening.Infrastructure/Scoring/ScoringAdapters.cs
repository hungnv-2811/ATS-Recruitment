using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ATS.AiScreening.Application.Scoring;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Ports;
using ATS.AiScreening.Domain.Services;

namespace ATS.AiScreening.Infrastructure.Scoring;

/// <summary>Ten tang — gia tri cot <c>adapter_used</c>, UI dung de gan nhan canh diem.</summary>
public static class AdapterNames
{
    public const string OpenAi = "OpenAI";
    public const string Embedding = "Embedding";
    public const string Keyword = "Keyword";
    public const string Fake = "Fake";
}

/// <summary>Chia ky nang bat buoc cua JD thanh "co trong CV" / "thieu" — dung chung cho cac adapter.</summary>
internal static class SkillMatch
{
    public static (IReadOnlyList<string> Matched, IReadOnlyList<string> Missing) Split(AnonymizedCv cv, IReadOnlyList<string> skills)
    {
        var matched = new List<string>();
        var missing = new List<string>();
        foreach (var skill in skills)
        {
            var hit = cv.Skills.Contains(SkillCatalog.Canonicalize(skill), StringComparer.OrdinalIgnoreCase)
                      || SkillCatalog.Mentions(cv.Text, skill);
            (hit ? matched : missing).Add(skill);
        }

        return (matched, missing);
    }
}

/// <summary>
/// Tang 3 — LUON chay duoc: khong mang, khong tien, khong ngau nhien.
/// </summary>
/// <remarks>
/// Cong thuc (ai-integration.md muc 1):
/// <c>diem = round(so ky nang bat buoc khop / tong * 100)</c>; JD khong liet ke ky nang nao thi 50 (trung tinh);
/// cong 5 diem moi ky nang uu tien khop (toi da +10); tru 10 neu so nam kinh nghiem trong CV
/// thap hon yeu cau. Kep trong 0–100. Day la thang diem KHAC OpenAI — UI phai gan nhan "Tu khoa".
/// </remarks>
public sealed class KeywordScoringAdapter : IAiScoringService
{
    public const int NeutralScore = 50;

    public Task<ScreeningResult> ScoreAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cv);
        ArgumentNullException.ThrowIfNull(jd);

        var (matched, missing) = SkillMatch.Split(cv, jd.RequiredSkills);
        var (niceMatched, _) = SkillMatch.Split(cv, jd.NiceToHaveSkills);

        var score = jd.RequiredSkills.Count == 0
            ? NeutralScore
            : (int)Math.Round(matched.Count * 100.0 / jd.RequiredSkills.Count, MidpointRounding.AwayFromZero);

        score += Math.Min(10, niceMatched.Count * 5);

        var gaps = missing.Select(s => $"Thiếu {s}").ToList();
        var cvYears = SkillCatalog.ExtractMaxYears(string.Join(" ", cv.ExperienceKeywords));
        if (jd.MinYearsExperience > 0 && cvYears < jd.MinYearsExperience)
        {
            score -= 10;
            gaps.Add(string.Create(CultureInfo.InvariantCulture, $"Chưa thấy đủ {jd.MinYearsExperience} năm kinh nghiệm"));
        }

        score = Math.Clamp(score, 0, 100);

        var strengths = matched.Concat(niceMatched).Select(s => $"Có {s}").ToArray();
        var summary = jd.RequiredSkills.Count == 0
            ? "JD không liệt kê kỹ năng cụ thể nên điểm từ khoá ở mức trung tính. Độ chính xác thấp — nên đọc CV trực tiếp."
            : string.Create(
                CultureInfo.InvariantCulture,
                $"Khớp {matched.Count}/{jd.RequiredSkills.Count} kỹ năng bắt buộc theo từ khoá. Độ chính xác thấp — nên đọc CV trực tiếp.");

        return Task.FromResult(new ScreeningResult(new Score(score), summary, strengths, gaps, AdapterNames.Keyword));
    }
}

/// <summary>
/// Adapter gia lap cho dev/test/demo (<c>AiProvider=Fake</c>): diem TAT DINH 40–95 tu hash noi dung,
/// cung CV+JD luon ra cung diem — khong ton tien, khong can mang.
/// </summary>
public sealed class FakeAiScoringAdapter : IAiScoringService
{
    public const int MinScore = 40;
    public const int MaxScore = 95;

    public Task<ScreeningResult> ScoreAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cv);
        ArgumentNullException.ThrowIfNull(jd);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(cv.Text + "\n" + JobRequirementParser.ToCacheText(jd)));
        var score = MinScore + (int)(BinaryPrimitives.ReadUInt32LittleEndian(hash) % (MaxScore - MinScore + 1));

        var (matched, missing) = SkillMatch.Split(cv, jd.RequiredSkills);
        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"[Fake] Điểm giả lập {score}/100 cho vị trí {jd.Title} — khớp {matched.Count}/{jd.RequiredSkills.Count} kỹ năng bắt buộc.");

        return Task.FromResult(new ScreeningResult(
            new Score(score),
            summary,
            matched.Select(s => $"Có {s}").ToArray(),
            missing.Select(s => $"Thiếu {s}").ToArray(),
            AdapterNames.Fake));
    }
}
