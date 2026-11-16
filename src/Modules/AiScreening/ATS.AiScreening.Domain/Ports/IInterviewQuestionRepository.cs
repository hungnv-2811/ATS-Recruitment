using ATS.AiScreening.Domain.Entities;

namespace ATS.AiScreening.Domain.Ports;

/// <summary>Bang <c>interview_questions</c> — bo cau hoi HR da luu cho mot buoi phong van.</summary>
public interface IInterviewQuestionRepository
{
    /// <summary>Theo dung thu tu <see cref="InterviewQuestionEntry.OrderIndex"/>.</summary>
    Task<IReadOnlyList<InterviewQuestionEntry>> ListByInterviewAsync(Guid interviewId, CancellationToken ct = default);

    /// <summary>Thay TOAN BO bo cau hoi cua buoi phong van (PUT) trong mot lan luu.</summary>
    Task ReplaceForInterviewAsync(Guid interviewId, IReadOnlyList<InterviewQuestionEntry> questions, CancellationToken ct = default);
}
