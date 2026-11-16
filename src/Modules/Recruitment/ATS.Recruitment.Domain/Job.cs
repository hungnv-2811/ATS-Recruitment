using ATS.SharedKernel;

namespace ATS.Recruitment.Domain;

public enum EmploymentType
{
    FullTime = 0,
    PartTime = 1,
    Internship = 2,
    Contract = 3,
}

/// <summary>Tin tuyen dung (recruitment.jobs). Vong doi: Draft → Open → Closed.</summary>
public sealed class Job : Entity<Guid>
{
    public const int TitleMaxLength = 200;
    public const int LocationMaxLength = 200;

    private Job()
    {
    }

    private Job(Guid id, Guid hrOwnerId, DateTimeOffset createdAt)
        : base(id)
    {
        HrOwnerId = hrOwnerId;
        CreatedAt = createdAt;
        Status = JobStatus.Draft;
    }

    /// <summary>Id trong identity.users cua HR so huu tin.</summary>
    public Guid HrOwnerId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Requirements { get; private set; } = string.Empty;

    public string Location { get; private set; } = string.Empty;

    public EmploymentType EmploymentType { get; private set; }

    public JobStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Lan dau mo tin (Draft → Open). Null khi con Draft.</summary>
    public DateTimeOffset? PostedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public static Result<Job> Create(
        Guid hrOwnerId,
        string title,
        string description,
        string requirements,
        string location,
        EmploymentType employmentType,
        bool publish,
        DateTimeOffset now)
    {
        var job = new Job(Guid.CreateVersion7(), hrOwnerId, now);
        var edit = job.Update(title, description, requirements, location, employmentType);
        if (edit.IsFailure)
        {
            return Result.Failure<Job>(edit.Error);
        }

        if (publish)
        {
            job.Publish(now);
        }

        return job;
    }

    /// <summary>Sua noi dung. Tin da Closed thi khong sua duoc nua.</summary>
    public Result Update(string title, string description, string requirements, string location, EmploymentType employmentType)
    {
        if (Status == JobStatus.Closed)
        {
            return Result.Failure(Error.Validation("Tin da dong, khong sua duoc."));
        }

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > TitleMaxLength)
        {
            return Result.Failure(Error.Validation($"Tieu de bat buoc, toi da {TitleMaxLength} ky tu."));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure(Error.Validation("Mo ta cong viec bat buoc."));
        }

        if ((location?.Trim().Length ?? 0) > LocationMaxLength)
        {
            return Result.Failure(Error.Validation($"Dia diem toi da {LocationMaxLength} ky tu."));
        }

        if (!Enum.IsDefined(employmentType))
        {
            return Result.Failure(Error.Validation("Hinh thuc lam viec khong hop le."));
        }

        Title = title.Trim();
        Description = description.Trim();
        Requirements = requirements?.Trim() ?? string.Empty;
        Location = location?.Trim() ?? string.Empty;
        EmploymentType = employmentType;
        return Result.Success();
    }

    /// <summary>Draft → Open.</summary>
    public Result Publish(DateTimeOffset now)
    {
        if (Status != JobStatus.Draft)
        {
            return Result.Failure(Error.Validation($"Chi tin Draft moi dang duoc (hien tai: {Status})."));
        }

        Status = JobStatus.Open;
        PostedAt ??= now;
        return Result.Success();
    }

    /// <summary>Draft/Open → Closed. Dong roi thi khong nhan don moi.</summary>
    public Result Close(DateTimeOffset now)
    {
        if (Status == JobStatus.Closed)
        {
            return Result.Failure(Error.Validation("Tin da dong."));
        }

        Status = JobStatus.Closed;
        ClosedAt = now;
        return Result.Success();
    }

    public bool IsOpen => Status == JobStatus.Open;
}
