using ATS.SharedKernel;

namespace ATS.Recruitment.Domain;

/// <summary>Ho so ung vien (recruitment.candidates), 1-1 voi tai khoan role=Candidate.</summary>
public sealed class Candidate : Entity<Guid>
{
    private Candidate()
    {
    }

    private Candidate(Guid id, Guid userId, string fullName, DateTimeOffset createdAt)
        : base(id)
    {
        UserId = userId;
        FullName = fullName;
        CreatedAt = createdAt;
    }

    /// <summary>Id trong identity.users.</summary>
    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public string? Address { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Candidate Create(Guid userId, string fullName, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        return new Candidate(Guid.CreateVersion7(), userId, fullName.Trim(), now);
    }
}

/// <summary>Ho so HR (recruitment.hr_profiles), 1-1 voi tai khoan role=HR.</summary>
public sealed class HrProfile : Entity<Guid>
{
    private HrProfile()
    {
    }

    private HrProfile(Guid id, Guid userId, string fullName)
        : base(id)
    {
        UserId = userId;
        FullName = fullName;
    }

    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public string? Department { get; private set; }

    public static HrProfile Create(Guid userId, string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        return new HrProfile(Guid.CreateVersion7(), userId, fullName.Trim());
    }
}
