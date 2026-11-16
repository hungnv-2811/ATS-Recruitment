using System.Text.Json;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Ports;
using ATS.AiScreening.Domain.Services;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace ATS.AiScreening.Infrastructure.Scoring;

/// <summary>
/// Tang 1 — gpt-4o-mini (mac dinh, <c>OpenAI:Model</c>), structured output JSON strict, prompt <c>scoring-v1</c>.
/// </summary>
/// <remarks>
/// Retry (3 lan, exponential backoff) va timeout mang do <c>OpenAIClientOptions</c> lo — cau hinh o
/// <see cref="AiScreeningModule"/>. Moi loi (HTTP, JSON hong, diem ngoai khoang) deu NEM ra de
/// <see cref="AiScoringPipeline"/> roi xuong tang sau.
/// </remarks>
public sealed class OpenAiScoringAdapter : IAiScoringService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ChatClient _chat;

    public OpenAiScoringAdapter(ChatClient chat) => _chat = chat;

    public async Task<ScreeningResult> ScoreAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cv);
        ArgumentNullException.ThrowIfNull(jd);

        ChatMessage[] messages =
        [
            new SystemChatMessage("Bạn chỉ trả lời bằng JSON đúng schema được yêu cầu."),
            new UserChatMessage(Prompts.BuildScoring(cv, jd)),
        ];

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "cv_jd_fit_score",
                BinaryData.FromString(Prompts.ScoringJsonSchema),
                jsonSchemaIsStrict: true),
            Temperature = 0.2f,
            MaxOutputTokenCount = 600,
        };

        var completion = await _chat.CompleteChatAsync(messages, options, ct).ConfigureAwait(false);
        var text = completion.Value.Content.Count > 0 ? completion.Value.Content[0].Text : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("OpenAI tra ve noi dung rong.");
        }

        var payload = JsonSerializer.Deserialize<ScoringPayload>(text, Json)
                      ?? throw new InvalidOperationException("OpenAI tra ve JSON khong doc duoc.");

        if (payload.Score is < 0 or > 100)
        {
            throw new InvalidOperationException($"OpenAI tra diem ngoai khoang 0-100: {payload.Score}.");
        }

        return new ScreeningResult(
            new Score(payload.Score),
            payload.Summary ?? string.Empty,
            payload.Strengths ?? [],
            payload.Gaps ?? [],
            AdapterNames.OpenAi);
    }

    private sealed record ScoringPayload(int Score, string? Summary, string[]? Strengths, string[]? Gaps);
}

/// <summary>
/// Tang 2 — text-embedding-3-small + cosine similarity giua CV va JD; <c>diem = round(sim * 100)</c>.
/// Re (~$0.00003/lan) va nhanh; khong co nhan xet ngu nghia nen summary la mau co dinh.
/// </summary>
public sealed class EmbeddingScoringAdapter : IAiScoringService
{
    private readonly EmbeddingClient _embeddings;

    public EmbeddingScoringAdapter(EmbeddingClient embeddings) => _embeddings = embeddings;

    public async Task<ScreeningResult> ScoreAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cv);
        ArgumentNullException.ThrowIfNull(jd);

        var jdText = string.Join(
            "\n",
            jd.Title,
            jd.Description,
            "Kỹ năng bắt buộc: " + string.Join(", ", jd.RequiredSkills),
            "Kỹ năng ưu tiên: " + string.Join(", ", jd.NiceToHaveSkills));

        var response = await _embeddings
            .GenerateEmbeddingsAsync([Prompts.Truncate(cv.Text), Prompts.Truncate(jdText)], cancellationToken: ct)
            .ConfigureAwait(false);

        var vectors = response.Value;
        if (vectors.Count < 2)
        {
            throw new InvalidOperationException("OpenAI embeddings tra thieu vector.");
        }

        var similarity = Cosine(vectors[0].ToFloats().Span, vectors[1].ToFloats().Span);
        var score = (int)Math.Round(Math.Clamp(similarity, 0, 1) * 100, MidpointRounding.AwayFromZero);

        var (matched, missing) = SkillMatch.Split(cv, jd.RequiredSkills);
        return new ScreeningResult(
            new Score(score),
            $"Độ tương đồng ngữ nghĩa giữa CV và JD là {score}/100 (embedding). Không có nhận xét chi tiết — nên đọc CV để đánh giá.",
            matched.Select(s => $"Có {s}").ToArray(),
            missing.Select(s => $"Thiếu {s}").ToArray(),
            AdapterNames.Embedding);
    }

    /// <summary>cos(a, b) = a·b / (|a||b|). Vector rong hoac bang 0 → 0.</summary>
    public static double Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length == 0 || a.Length != b.Length)
        {
            return 0;
        }

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * (double)b[i];
            normA += a[i] * (double)a[i];
            normB += b[i] * (double)b[i];
        }

        return normA == 0 || normB == 0 ? 0 : dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}

/// <summary>Sinh cau hoi phong van bang gpt-4o-mini, prompt <c>questions-v1</c>, JSON strict. Loi thi nem — pipeline roi xuong Template.</summary>
public sealed class OpenAiInterviewQuestionAdapter : IInterviewQuestionGenerator
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ChatClient _chat;

    public OpenAiInterviewQuestionAdapter(ChatClient chat) => _chat = chat;

    public async Task<IReadOnlyList<InterviewQuestion>> GenerateAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cv);
        ArgumentNullException.ThrowIfNull(jd);

        ChatMessage[] messages =
        [
            new SystemChatMessage("Bạn chỉ trả lời bằng JSON đúng schema được yêu cầu."),
            new UserChatMessage(Prompts.BuildQuestions(cv, jd)),
        ];

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "interview_questions",
                BinaryData.FromString(Prompts.QuestionsJsonSchema),
                jsonSchemaIsStrict: true),
            Temperature = 0.5f,
            MaxOutputTokenCount = 1500,
        };

        var completion = await _chat.CompleteChatAsync(messages, options, ct).ConfigureAwait(false);
        var text = completion.Value.Content.Count > 0 ? completion.Value.Content[0].Text : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("OpenAI tra ve noi dung rong.");
        }

        var payload = JsonSerializer.Deserialize<QuestionsPayload>(text, Json);
        var questions = (payload?.Questions ?? [])
            .Where(q => !string.IsNullOrWhiteSpace(q.Question)
                        && Domain.Entities.InterviewQuestionEntry.IsValidCategory(q.Category))
            .Select(q => new InterviewQuestion(q.Category!.Trim().ToLowerInvariant(), q.Question!.Trim(), "AI"))
            .ToArray();

        return questions.Length == 0
            ? throw new InvalidOperationException("OpenAI khong tra ve cau hoi hop le nao.")
            : questions;
    }

    private sealed record QuestionsPayload(QuestionPayload[]? Questions);

    private sealed record QuestionPayload(string? Category, string? Question);
}

/// <summary>
/// Fallback sinh cau hoi KHONG can mang: 10 cau tu mau theo ky nang cua JD
/// (6 technical, 2 behavioral, 2 situational). Source = "Template".
/// </summary>
public sealed class TemplateInterviewQuestionAdapter : IInterviewQuestionGenerator
{
    public const int QuestionCount = 10;
    private const int TechnicalCount = 6;

    private static readonly string[] SkillTemplates =
    [
        "Hãy mô tả một dự án gần nhất bạn dùng {0}. Bạn chịu trách nhiệm phần nào và kết quả ra sao?",
        "Khi làm việc với {0}, bạn đã gặp vấn đề kỹ thuật khó nhất nào và giải quyết thế nào?",
        "Bạn đánh giá điểm mạnh và hạn chế của {0} như thế nào so với các lựa chọn thay thế?",
        "Làm sao bạn bảo đảm chất lượng và hiệu năng khi xây dựng tính năng bằng {0}?",
        "Nếu phải hướng dẫn một thành viên mới làm quen với {0}, bạn sẽ bắt đầu từ đâu?",
        "Hãy giải thích một khái niệm cốt lõi của {0} mà bạn thấy người mới hay hiểu sai.",
    ];

    private static readonly string[] GenericTechnical =
    [
        "Hãy mô tả kiến trúc của hệ thống gần nhất bạn tham gia cho vị trí tương tự {0}.",
        "Bạn thường debug một lỗi chỉ xảy ra trên môi trường production như thế nào?",
        "Bạn viết test cho phần việc của mình theo cách nào? Cho ví dụ cụ thể.",
        "Khi review code của đồng nghiệp, bạn chú ý những điểm gì nhất?",
        "Hãy kể về một lần bạn tối ưu hiệu năng: đo bằng gì và cải thiện được bao nhiêu?",
        "Bạn cập nhật kiến thức chuyên môn cho vị trí {0} bằng cách nào?",
    ];

    private static readonly string[] Behavioral =
    [
        "Hãy kể về một lần bạn bất đồng ý kiến với đồng nghiệp trong nhóm. Bạn đã xử lý thế nào?",
        "Kể về một dự án bị trễ hạn bạn từng tham gia. Bạn đã làm gì và rút ra bài học gì?",
    ];

    private static readonly string[] Situational =
    [
        "Giả sử bạn nhận yêu cầu gấp từ hai bên liên quan cùng lúc với vị trí {0}. Bạn ưu tiên thế nào?",
        "Nếu phát hiện lỗi nghiêm trọng ngay trước buổi phát hành, bạn sẽ làm gì?",
    ];

    public Task<IReadOnlyList<InterviewQuestion>> GenerateAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cv);
        ArgumentNullException.ThrowIfNull(jd);

        // Uu tien ky nang JD ma CV CO nhac toi (prompt v1: "bam sat ky nang ung vien da ghi"), roi toi ky nang con lai.
        var skills = jd.RequiredSkills
            .Concat(jd.NiceToHaveSkills)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(s => SkillCatalog.Mentions(cv.Text, s))
            .Take(TechnicalCount)
            .ToArray();

        var title = string.IsNullOrWhiteSpace(jd.Title) ? "này" : jd.Title;
        var questions = new List<InterviewQuestion>(QuestionCount);

        for (var i = 0; i < skills.Length; i++)
        {
            questions.Add(Technical(SkillTemplates[i % SkillTemplates.Length], skills[i]));
        }

        for (var i = 0; questions.Count < TechnicalCount; i++)
        {
            questions.Add(Technical(GenericTechnical[i % GenericTechnical.Length], title));
        }

        questions.AddRange(Behavioral.Select(q => new InterviewQuestion("behavioral", q, "Template")));
        questions.AddRange(Situational.Select(q => new InterviewQuestion("situational", Format(q, title), "Template")));

        return Task.FromResult<IReadOnlyList<InterviewQuestion>>(questions);
    }

    private static InterviewQuestion Technical(string template, string value)
        => new("technical", Format(template, value), "Template");

    private static string Format(string template, string value)
        => template.Replace("{0}", value, StringComparison.Ordinal);
}
