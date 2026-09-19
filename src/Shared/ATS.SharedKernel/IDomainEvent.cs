namespace ATS.SharedKernel;

/// <summary>Su kien trong domain. Chua dung o phase 1, giu cho ve sau.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
