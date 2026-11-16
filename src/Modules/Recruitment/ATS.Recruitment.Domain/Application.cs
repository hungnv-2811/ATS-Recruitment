using ATS.SharedKernel;

namespace ATS.Recruitment.Domain;

/// <summary>
/// Don ung tuyen (recruitment.applications): ung vien + tin + CV da chon + trang thai nghiep vu.
/// </summary>
/// <remarks>
/// State machine (use-cases.md muc 4): <c>Applied → Screening → Interview → Hired</c>, tu moi
/// trang thai chua ket thuc co the sang <c>Rejected</c>; khong nhay coc. Trang thai nay DOC LAP
/// voi trang thai cham AI (RB9) — diem AI khong bao gio tu chuyen no.
/// Ngoai le co chu dich duy nhat: hen phong van dua don Applied/Screening thang sang Interview
/// (<see cref="EnterInterviewStage"/>, contracts.md 8.3).
/// </remarks>
public sealed class Application : Entity<Guid>
{
    public const int HrNoteMaxLength = 4000;

    private Application()
    {
    }

    private Application(Guid id, Guid jobId, Guid candidateId, Guid cvId, DateTimeOffset now)
        : base(id)
    {
        JobId = jobId;
        CandidateId = candidateId;
        CvId = cvId;
        Status = ApplicationStatus.Applied;
        AppliedAt = now;
        UpdatedAt = now;
    }

    public Guid JobId { get; private set; }

    public Guid CandidateId { get; private set; }

    public Guid CvId { get; private set; }

    public ApplicationStatus Status { get; private set; }

    /// <summary>Ghi chu rieng cua HR — ung vien khong thay.</summary>
    public string? HrNote { get; private set; }

    public DateTimeOffset AppliedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsFinal => Status is ApplicationStatus.Hired or ApplicationStatus.Rejected;

    public static Application Submit(Guid jobId, Guid candidateId, Guid cvId, DateTimeOffset now)
        => new(Guid.CreateVersion7(), jobId, candidateId, cvId, now);

    /// <summary>Buoc ke tiep hop le duy nhat tren nhanh chinh (null neu da ket thuc / Interview→Hired la cuoi).</summary>
    public static ApplicationStatus? NextOnMainPath(ApplicationStatus from) => from switch
    {
        ApplicationStatus.Applied => ApplicationStatus.Screening,
        ApplicationStatus.Screening => ApplicationStatus.Interview,
        ApplicationStatus.Interview => ApplicationStatus.Hired,
        _ => null,
    };

    public static bool CanTransition(ApplicationStatus from, ApplicationStatus to)
    {
        if (from is ApplicationStatus.Hired or ApplicationStatus.Rejected)
        {
            return false;
        }

        return to == ApplicationStatus.Rejected || NextOnMainPath(from) == to;
    }

    /// <summary>HR chuyen trang thai theo dung luat; kem ghi chu (tuy chon).</summary>
    public Result ChangeStatus(ApplicationStatus to, string? note, DateTimeOffset now)
    {
        if (!Enum.IsDefined(to))
        {
            return Result.Failure(Error.Validation("Trang thai khong hop le."));
        }

        if (!CanTransition(Status, to))
        {
            return Result.Failure(Error.Validation($"Khong the chuyen tu {Status} sang {to}."));
        }

        var noteResult = SetNote(note);
        if (noteResult.IsFailure)
        {
            return noteResult;
        }

        Status = to;
        UpdatedAt = now;
        return Result.Success();
    }

    /// <summary>Hen phong van: Applied/Screening → Interview; dang Interview thi giu nguyen.</summary>
    public Result EnterInterviewStage(DateTimeOffset now)
    {
        if (IsFinal)
        {
            return Result.Failure(Error.Validation($"Don da ket thuc ({Status}), khong hen phong van duoc."));
        }

        if (Status != ApplicationStatus.Interview)
        {
            Status = ApplicationStatus.Interview;
            UpdatedAt = now;
        }

        return Result.Success();
    }

    private Result SetNote(string? note)
    {
        if (note is null)
        {
            return Result.Success();
        }

        if (note.Length > HrNoteMaxLength)
        {
            return Result.Failure(Error.Validation($"Ghi chu toi da {HrNoteMaxLength} ky tu."));
        }

        HrNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        return Result.Success();
    }
}
