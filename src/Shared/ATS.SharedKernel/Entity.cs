namespace ATS.SharedKernel;

/// <summary>
/// Lop co so cho Entity: co dinh danh, so sanh theo Id chu khong theo gia tri.
/// </summary>
public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    /// <summary>Danh cho EF Core materialize.</summary>
    protected Entity() => Id = default!;

    public TId Id { get; protected set; }

    public override bool Equals(object? obj)
        => obj is Entity<TId> other
           && other.GetType() == GetType()
           && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
