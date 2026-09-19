using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ATS.Persistence;

/// <summary>
/// Chi dung luc THIET KE (dotnet ef migrations add / script). Khong chay luc runtime.
/// </summary>
/// <remarks>
/// Nho co lop nay ma tao migration khong can den ATS.Api: khong phai keo goi
/// Microsoft.EntityFrameworkCore.Design vao project host chi de chay cong cu.
///
/// Chuoi ket noi o day KHONG can tro toi database that — EF chi doc no de biet
/// dung provider nao khi sinh SQL.
///
/// Cach dung (tu thu muc goc repo):
///   dotnet tool restore
///   dotnet dotnet-ef migrations add TenMigration --project src/Shared/ATS.Persistence
/// </remarks>
public sealed class AtsDbContextFactory : IDesignTimeDbContextFactory<AtsDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=ats;Username=ats;Password=design-time-only";

    public AtsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                               ?? DesignTimeConnectionString;

        var options = new DbContextOptionsBuilder<AtsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AtsDbContext(options);
    }
}
