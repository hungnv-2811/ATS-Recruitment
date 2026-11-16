using ATS.AiScreening.Application.Contracts;
using ATS.AiScreening.Application.Ports;
using ATS.AiScreening.Application.Scoring;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Entities;
using ATS.AiScreening.Domain.Ports;
using ATS.SharedKernel;

namespace ATS.AiScreening.Application.Handlers;

/// <summary>
/// HR sinh bo cau hoi phong van cho mot don (CHUA luu — HR xem, sua, roi moi PUT de luu).
/// </summary>
/// <remarks>
/// CV khong trich duoc chu van sinh duoc: dung mot doan giu cho thay CV, generator (Template)
/// van ra cau hoi theo ky nang cua JD. CV van di qua IAnonymizer nhu moi duong khac (ADR-3).
/// </remarks>
public sealed class GenerateQuestionsHandler
{
    private const string EmptyCvPlaceholder = "(CV khong co noi dung van ban)";

    private readonly IRecruitmentReader _recruitment;
    private readonly IAnonymizer _anonymizer;
    private readonly IInterviewQuestionGenerator _generator;

    public GenerateQuestionsHandler(
        IRecruitmentReader recruitment,
        IAnonymizer anonymizer,
        IInterviewQuestionGenerator generator)
    {
        _recruitment = recruitment;
        _anonymizer = anonymizer;
        _generator = generator;
    }

    public async Task<Result<IReadOnlyList<InterviewQuestionDto>>> HandleAsync(
        Guid hrUserId,
        Guid applicationId,
        CancellationToken ct = default)
    {
        var application = await _recruitment.GetApplicationAsync(applicationId, ct).ConfigureAwait(false);
        if (application is null)
        {
            return Result.Failure<IReadOnlyList<InterviewQuestionDto>>(Error.NotFound("Khong tim thay don ung tuyen."));
        }

        if (application.Job.HrOwnerUserId != hrUserId)
        {
            return Result.Failure<IReadOnlyList<InterviewQuestionDto>>(Error.Forbidden("Chi HR so huu tin moi duoc sinh cau hoi."));
        }

        var rawCv = string.IsNullOrWhiteSpace(application.Cv.ExtractedText)
            ? EmptyCvPlaceholder
            : application.Cv.ExtractedText;

        var anonymized = await _anonymizer.AnonymizeAsync(rawCv, ct).ConfigureAwait(false);
        var jd = JobRequirementParser.Parse(application.Job);
        var questions = await _generator.GenerateAsync(anonymized, jd, ct).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<InterviewQuestionDto>>(questions.Select(InterviewQuestionDto.From).ToArray());
    }
}

/// <summary>Luu / doc bo cau hoi da chot cho mot buoi phong van. Chi HR so huu tin.</summary>
public sealed class InterviewQuestionsHandler
{
    public const int MaxQuestions = 30;

    private readonly IRecruitmentReader _recruitment;
    private readonly IInterviewQuestionRepository _questions;
    private readonly TimeProvider _clock;

    public InterviewQuestionsHandler(
        IRecruitmentReader recruitment,
        IInterviewQuestionRepository questions,
        TimeProvider clock)
    {
        _recruitment = recruitment;
        _questions = questions;
        _clock = clock;
    }

    public async Task<Result> SaveAsync(Guid hrUserId, Guid interviewId, SaveQuestionsRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = await CheckOwnerAsync(hrUserId, interviewId, ct).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return access;
        }

        var items = request.Questions ?? [];
        if (items.Count > MaxQuestions)
        {
            return Result.Failure(Error.Validation($"Toi da {MaxQuestions} cau hoi cho mot buoi phong van."));
        }

        for (var i = 0; i < items.Count; i++)
        {
            var q = items[i];
            if (q is null || string.IsNullOrWhiteSpace(q.Question))
            {
                return Result.Failure(Error.Validation($"Cau hoi thu {i + 1} dang trong."));
            }

            if (q.Question.Trim().Length > InterviewQuestionEntry.MaxQuestionLength)
            {
                return Result.Failure(Error.Validation($"Cau hoi thu {i + 1} dai qua {InterviewQuestionEntry.MaxQuestionLength} ky tu."));
            }

            if (!InterviewQuestionEntry.IsValidCategory(q.Category))
            {
                return Result.Failure(Error.Validation(
                    $"Cau hoi thu {i + 1}: category phai la technical, behavioral hoac situational."));
            }
        }

        var now = _clock.GetUtcNow();
        var entries = items
            .Select((q, index) => InterviewQuestionEntry.Create(
                interviewId,
                new InterviewQuestion(q.Category, q.Question, q.Source),
                index,
                now))
            .ToArray();

        await _questions.ReplaceForInterviewAsync(interviewId, entries, ct).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<InterviewQuestionDto>>> ListAsync(Guid hrUserId, Guid interviewId, CancellationToken ct = default)
    {
        var access = await CheckOwnerAsync(hrUserId, interviewId, ct).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result.Failure<IReadOnlyList<InterviewQuestionDto>>(access.Error);
        }

        var entries = await _questions.ListByInterviewAsync(interviewId, ct).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<InterviewQuestionDto>>(
            entries.Select(e => InterviewQuestionDto.From(e.ToValue())).ToArray());
    }

    private async Task<Result> CheckOwnerAsync(Guid hrUserId, Guid interviewId, CancellationToken ct)
    {
        var applicationId = await _recruitment.GetApplicationIdOfInterviewAsync(interviewId, ct).ConfigureAwait(false);
        if (applicationId is null)
        {
            return Result.Failure(Error.NotFound("Khong tim thay buoi phong van."));
        }

        var application = await _recruitment.GetApplicationAsync(applicationId.Value, ct).ConfigureAwait(false);
        if (application is null)
        {
            return Result.Failure(Error.NotFound("Khong tim thay don ung tuyen cua buoi phong van."));
        }

        return application.Job.HrOwnerUserId == hrUserId
            ? Result.Success()
            : Result.Failure(Error.Forbidden("Chi HR so huu tin moi xem/sua duoc cau hoi phong van."));
    }
}
