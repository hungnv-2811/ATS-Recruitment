namespace ATS.Persistence;

/// <summary>
/// Du lieu demo (docs/database-design.md muc 6) — TV1 so huu.
/// </summary>
/// <remarks>
/// Chay sau MigrateAsync khi Database:SeedDemoData=true. Phai IDEMPOTENT: khoi dong lai
/// container khong duoc nhan doi du lieu.
/// </remarks>
public static class DatabaseSeeder
{
    public static Task SeedAsync(AtsDbContext db, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        return Task.CompletedTask;
    }
}
