using Microsoft.EntityFrameworkCore;

namespace ATS.Persistence;

/// <summary>
/// MOT DbContext dung chung cho ca 3 module, tach ban theo schema PostgreSQL.
/// Xem docs/architecture.md muc 3.2 de biet vi sao khong dung DbContext-per-module.
/// </summary>
/// <remarks>
/// Project nay ton tai vi mot DbContext dung chung can mot cho o chung: neu dat no
/// trong Infrastructure cua mot module thi hai module con lai phai tham chieu cheo,
/// vi pham ranh gioi module (muc 3.3).
///
/// Cau hinh entity KHONG nam o day. Moi module tu viet IEntityTypeConfiguration
/// trong Infrastructure cua minh, va duoc nap bang ApplyConfigurationsFromAssembly
/// o <see cref="AtsDbContextConfigurator"/>.
/// </remarks>
public class AtsDbContext : DbContext
{
    public const string IdentitySchema = "identity";
    public const string RecruitmentSchema = "recruitment";
    public const string AiScreeningSchema = "aiscreening";

    public AtsDbContext(DbContextOptions<AtsDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tuan 2 chua co entity nao. Migration dau tien chi tao 3 schema.
        // Tu tuan 3 tro di, moi module them cau hinh cua minh qua
        // AtsDbContextConfigurator.ApplyModuleConfigurations.
        AtsDbContextConfigurator.ApplyModuleConfigurations(modelBuilder);
    }
}
