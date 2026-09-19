namespace ATS.AiScreening.Domain.Ports;

/// <summary>Sinh bo cau hoi phong van tu JD + CV. HR dung khi hen phong van.</summary>
/// <remarks>Cung rang buoc voi <see cref="IAiScoringService"/>: chi nhan <see cref="AnonymizedCv"/>.</remarks>
public interface IInterviewQuestionGenerator
{
    Task<IReadOnlyList<InterviewQuestion>> GenerateAsync(
        AnonymizedCv cv,
        JobRequirement jd,
        CancellationToken ct = default);
}
