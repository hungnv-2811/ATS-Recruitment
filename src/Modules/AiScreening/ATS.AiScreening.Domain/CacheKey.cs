using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ATS.AiScreening.Domain;

/// <summary>
/// Khoa cache theo NOI DUNG (CV + JD + model + prompt) — khong gan voi don ung tuyen nao.
/// Day la ly do bang <c>ai_score_cache</c> phai tach khoi <c>ai_scores</c>:
/// ung vien xem preview khi CHUA nop don (UC-05), luc do chua co Application de tham chieu.
/// </summary>
public sealed partial record CacheKey(string Value)
{
    public static CacheKey From(
        string cvText,
        string jdText,
        string modelVersion,
        string promptVersion)
    {
        var raw = string.Join(
            '|',
            Normalize(cvText),
            Normalize(jdText),
            modelVersion,
            promptVersion);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return new CacheKey(Convert.ToHexString(hash).ToLowerInvariant());
    }

    /// <summary>Chuan hoa de thay doi vo nghia (hoa/thuong, khoang trang) khong lam truot cache.</summary>
    private static string Normalize(string text)
        => WhitespaceRegex().Replace(text.Trim().ToLower(CultureInfo.InvariantCulture), " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
