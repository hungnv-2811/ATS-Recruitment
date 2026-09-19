namespace ATS.SharedKernel;

/// <summary>Ghi mot loat thay doi trong cung mot transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
