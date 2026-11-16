using ATS.SharedKernel;

namespace ATS.Recruitment.Domain;

public enum InterviewStatus
{
    Scheduled = 0,
    Done = 1,
    Cancelled = 2,
}

public enum Recommendation
{
    Hire = 0,
    Reject = 1,
    Consider = 2,
}

/// <summary>Buoi phong van cua mot don (recruitment.interviews).</summary>
/// <remarks>
/// <see cref="EmailSent"/> = false khi gui loi moi that bai: buoi phong van VAN duoc luu, HR co
/// nut "Gui lai loi moi" (UC-04). Email khong bao gio nam trong transaction cua nghiep vu.
/// </remarks>
public sealed class Interview : Entity<Guid>
{
    public const string ModeOnline = "online";
    public const string ModeOnsite = "onsite";

    private Interview()
    {
    }

    private Interview(Guid id, Guid applicationId, DateTimeOffset scheduledAt, string mode, string locationOrLink, string interviewerName, DateTimeOffset createdAt)
        : base(id)
    {
        ApplicationId = applicationId;
        ScheduledAt = scheduledAt;
        Mode = mode;
        LocationOrLink = locationOrLink;
        InterviewerName = interviewerName;
        Status = InterviewStatus.Scheduled;
        CreatedAt = createdAt;
    }

    public Guid ApplicationId { get; private set; }

    public DateTimeOffset ScheduledAt { get; private set; }

    /// <summary>"online" | "onsite".</summary>
    public string Mode { get; private set; } = ModeOnline;

    public string LocationOrLink { get; private set; } = string.Empty;

    public string InterviewerName { get; private set; } = string.Empty;

    public InterviewStatus Status { get; private set; }

    public bool EmailSent { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Interview> Schedule(
        Guid applicationId,
        DateTimeOffset scheduledAt,
        string? mode,
        string? locationOrLink,
        string? interviewerName,
        DateTimeOffset now)
    {
        var normalizedMode = mode?.Trim().ToLowerInvariant();
        if (normalizedMode is not (ModeOnline or ModeOnsite))
        {
            return Result.Failure<Interview>(Error.Validation("Hinh thuc phai la 'online' hoac 'onsite'."));
        }

        if (scheduledAt <= now)
        {
            return Result.Failure<Interview>(Error.Validation("Thoi gian phong van phai o tuong lai."));
        }

        if (string.IsNullOrWhiteSpace(interviewerName) || interviewerName.Trim().Length > 200)
        {
            return Result.Failure<Interview>(Error.Validation("Ten nguoi phong van bat buoc, toi da 200 ky tu."));
        }

        if (string.IsNullOrWhiteSpace(locationOrLink) || locationOrLink.Trim().Length > 500)
        {
            return Result.Failure<Interview>(Error.Validation("Dia diem/link bat buoc, toi da 500 ky tu."));
        }

        return new Interview(
            Guid.CreateVersion7(),
            applicationId,
            scheduledAt.ToUniversalTime(),
            normalizedMode,
            locationOrLink.Trim(),
            interviewerName.Trim(),
            now);
    }

    public void MarkEmailSent(bool sent) => EmailSent = sent;

    /// <summary>Scheduled → Done (khi HR nhap danh gia).</summary>
    public Result Complete()
    {
        if (Status != InterviewStatus.Scheduled)
        {
            return Result.Failure(Error.Validation($"Buoi phong van dang o trang thai {Status}."));
        }

        Status = InterviewStatus.Done;
        return Result.Success();
    }
}

/// <summary>Danh gia sau phong van (recruitment.evaluations), toi da 1 / buoi.</summary>
public sealed class Evaluation : Entity<Guid>
{
    private Evaluation()
    {
    }

    private Evaluation(Guid id, Guid interviewId, int rating, string? comment, Recommendation recommendation, DateTimeOffset createdAt)
        : base(id)
    {
        InterviewId = interviewId;
        Rating = rating;
        Comment = comment;
        Recommendation = recommendation;
        CreatedAt = createdAt;
    }

    public Guid InterviewId { get; private set; }

    /// <summary>1–5.</summary>
    public int Rating { get; private set; }

    public string? Comment { get; private set; }

    public Recommendation Recommendation { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Evaluation> Create(Guid interviewId, int rating, string? comment, Recommendation recommendation, DateTimeOffset now)
    {
        if (rating is < 1 or > 5)
        {
            return Result.Failure<Evaluation>(Error.Validation("Diem danh gia tu 1 den 5."));
        }

        if (!Enum.IsDefined(recommendation))
        {
            return Result.Failure<Evaluation>(Error.Validation("De xuat phai la Hire, Reject hoac Consider."));
        }

        if (comment is { Length: > 4000 })
        {
            return Result.Failure<Evaluation>(Error.Validation("Nhan xet toi da 4000 ky tu."));
        }

        return new Evaluation(
            Guid.CreateVersion7(),
            interviewId,
            rating,
            string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            recommendation,
            now);
    }
}
