using ATS.SharedKernel;

namespace ATS.AiScreening.Domain.Entities;

/// <summary>
/// Cau hoi phong van DA LUU cho mot buoi phong van (bang <c>aiscreening.interview_questions</c>).
/// </summary>
/// <remarks>
/// Ten co hau to "Entry" de khong dung voi value object <see cref="InterviewQuestion"/> ma port
/// <see cref="Ports.IInterviewQuestionGenerator"/> tra ve: cau hoi vua SINH (chua luu) la value,
/// cau hoi HR da chon/sua roi LUU la entity, co thu tu (<see cref="OrderIndex"/>).
/// </remarks>
public sealed class InterviewQuestionEntry : Entity<Guid>
{
    public const int MaxQuestionLength = 1000;

    private static readonly string[] Categories = ["technical", "behavioral", "situational"];
    private static readonly string[] Sources = ["AI", "Manual", "Template"];

    private InterviewQuestionEntry(Guid id, Guid interviewId, string category, string question, string source, int orderIndex, DateTimeOffset now)
        : base(id)
    {
        InterviewId = interviewId;
        Category = category;
        Question = question;
        Source = source;
        OrderIndex = orderIndex;
        CreatedAt = now;
    }

    /// <summary>Danh cho EF Core.</summary>
    private InterviewQuestionEntry()
    {
        Category = string.Empty;
        Question = string.Empty;
        Source = string.Empty;
    }

    /// <summary>Id buoi phong van (recruitment.interviews) — chi la Guid, khong FK xuyen module trong code.</summary>
    public Guid InterviewId { get; private set; }

    /// <summary>"technical" | "behavioral" | "situational".</summary>
    public string Category { get; private set; }

    public string Question { get; private set; }

    /// <summary>"AI" | "Manual" | "Template".</summary>
    public string Source { get; private set; }

    public int OrderIndex { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static bool IsValidCategory(string? category)
        => category is not null && Categories.Contains(category.Trim().ToLowerInvariant(), StringComparer.Ordinal);

    public static InterviewQuestionEntry Create(Guid interviewId, InterviewQuestion question, int orderIndex, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(question);
        ArgumentException.ThrowIfNullOrWhiteSpace(question.Question);
        ArgumentOutOfRangeException.ThrowIfNegative(orderIndex);

        var category = question.Category.Trim().ToLowerInvariant();
        if (!Categories.Contains(category, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Category '{question.Category}' khong hop le.", nameof(question));
        }

        var text = question.Question.Trim();
        if (text.Length > MaxQuestionLength)
        {
            throw new ArgumentException($"Cau hoi dai qua {MaxQuestionLength} ky tu.", nameof(question));
        }

        var source = Sources.FirstOrDefault(s => string.Equals(s, question.Source?.Trim(), StringComparison.OrdinalIgnoreCase))
                     ?? "Manual";

        return new InterviewQuestionEntry(Guid.CreateVersion7(), interviewId, category, text, source, orderIndex, now);
    }

    public InterviewQuestion ToValue() => new(Category, Question, Source);
}
