using System.Globalization;
using ATS.AiScreening.Domain;

namespace ATS.AiScreening.Infrastructure.Scoring;

/// <summary>
/// Prompt v1 — nguyen van docs/ai-integration.md muc 2. Doi noi dung = doi version
/// (<c>scoring-v2</c>...), vi version nam trong CacheKey: khong doi thi cache cu tra ket qua cua prompt da bo.
/// </summary>
/// <remarks>Chi <see cref="AnonymizedCv.Text"/> duoc dua vao prompt, khong bao gio CV tho (RB3).</remarks>
internal static class Prompts
{
    public const string ScoringVersion = "scoring-v1";
    public const string QuestionsVersion = "questions-v1";

    /// <summary>Gioi han do dai CV dua vao prompt (~3–4k token) — chan chi phi bat thuong.</summary>
    public const int MaxCvChars = 12_000;

    private const string ScoringTemplate = """
        Bạn là chuyên gia tuyển dụng. Chấm điểm mức độ phù hợp giữa CV và mô tả công việc,
        theo thang 0-100. Trả về JSON strict:

        {
          "score": <0-100>,
          "summary": "<1-2 câu tổng kết>",
          "strengths": ["<điểm mạnh 1>", "<điểm mạnh 2>", ...],
          "gaps": ["<kỹ năng thiếu 1>", ...]
        }

        Quy tắc chấm:
        - 90-100: khớp hoàn toàn, có thể vào phỏng vấn ngay
        - 70-89: khớp phần lớn, thiếu 1-2 kỹ năng phụ
        - 50-69: khớp một nửa, cần training
        - <50: không phù hợp

        Không được nêu tên riêng của ứng viên hoặc công ty trong summary.
        Không được đề xuất mức lương.

        --- CV (đã ẩn danh) ---
        {{cv_text}}

        --- JD ---
        Vị trí: {{job_title}}
        Yêu cầu: {{job_requirements}}
        Kỹ năng bắt buộc: {{required_skills}}
        Kỹ năng ưu tiên: {{nice_to_have_skills}}
        Kinh nghiệm tối thiểu: {{min_years}} năm
        """;

    private const string QuestionsTemplate = """
        Bạn là chuyên gia phỏng vấn. Dựa trên CV (đã ẩn danh) và JD, sinh 8-12 câu hỏi phỏng vấn.
        Chia làm 3 nhóm:
        - technical (5-7 câu): kiểm tra kỹ năng chuyên môn cụ thể có trong CV & JD
        - behavioral (2-3 câu): kiểm tra soft skill, kinh nghiệm làm việc nhóm
        - situational (1-2 câu): tình huống cụ thể khi làm ở vị trí này

        Trả về JSON strict:
        {
          "questions": [
            {"category": "technical", "question": "..."},
            ...
          ]
        }

        Quy tắc:
        - Không câu hỏi yes/no
        - Câu hỏi phải bám sát kỹ năng ứng viên đã ghi trong CV (đừng hỏi thứ họ không đề cập)
        - Không hỏi thông tin cá nhân, tuổi tác, tôn giáo, hôn nhân
        - Câu hỏi bằng tiếng Việt

        --- CV (đã ẩn danh) ---
        {{cv_text}}

        --- JD ---
        Vị trí: {{job_title}}
        Yêu cầu: {{job_requirements}}
        """;

    public const string ScoringJsonSchema = """
        {
          "type": "object",
          "properties": {
            "score": { "type": "integer" },
            "summary": { "type": "string" },
            "strengths": { "type": "array", "items": { "type": "string" } },
            "gaps": { "type": "array", "items": { "type": "string" } }
          },
          "required": ["score", "summary", "strengths", "gaps"],
          "additionalProperties": false
        }
        """;

    public const string QuestionsJsonSchema = """
        {
          "type": "object",
          "properties": {
            "questions": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "category": { "type": "string", "enum": ["technical", "behavioral", "situational"] },
                  "question": { "type": "string" }
                },
                "required": ["category", "question"],
                "additionalProperties": false
              }
            }
          },
          "required": ["questions"],
          "additionalProperties": false
        }
        """;

    public static string BuildScoring(AnonymizedCv cv, JobRequirement jd)
        => ScoringTemplate
            .Replace("{{cv_text}}", Truncate(cv.Text), StringComparison.Ordinal)
            .Replace("{{job_title}}", jd.Title, StringComparison.Ordinal)
            .Replace("{{job_requirements}}", jd.Description, StringComparison.Ordinal)
            .Replace("{{required_skills}}", JoinOrNone(jd.RequiredSkills), StringComparison.Ordinal)
            .Replace("{{nice_to_have_skills}}", JoinOrNone(jd.NiceToHaveSkills), StringComparison.Ordinal)
            .Replace("{{min_years}}", jd.MinYearsExperience.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

    public static string BuildQuestions(AnonymizedCv cv, JobRequirement jd)
        => QuestionsTemplate
            .Replace("{{cv_text}}", Truncate(cv.Text), StringComparison.Ordinal)
            .Replace("{{job_title}}", jd.Title, StringComparison.Ordinal)
            .Replace(
                "{{job_requirements}}",
                $"{jd.Description}\nKỹ năng: {JoinOrNone(jd.RequiredSkills.Concat(jd.NiceToHaveSkills).ToArray())}",
                StringComparison.Ordinal);

    public static string Truncate(string text) => text.Length <= MaxCvChars ? text : text[..MaxCvChars];

    private static string JoinOrNone(IReadOnlyList<string> items) => items.Count == 0 ? "(không nêu)" : string.Join(", ", items);
}
