using ATS.SharedKernel;
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
/// trong Infrastructure cua minh; danh sach assembly duoc TRUYEN VAO qua
/// <see cref="ModuleAssemblies"/>, khong phai dang ky ngam vao mot bien static.
/// Nho vay khong the dung duoc DbContext nay ma thieu danh sach module.
/// </remarks>
public class AtsDbContext : DbContext, IUnitOfWork
{
    public const string IdentitySchema = "identity";
    public const string RecruitmentSchema = "recruitment";
    public const string AiScreeningSchema = "aiscreening";

    private readonly ModuleAssemblies _modules;

    public AtsDbContext(DbContextOptions<AtsDbContext> options, ModuleAssemblies modules)
        : base(options)
        => _modules = modules;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tuan 2 chua module nao co entity, nen vong lap nay chua sinh ra bang gi.
        // Tu tuan 3, moi IEntityTypeConfiguration dat trong *.Infrastructure se
        // duoc nap tu dong ma AtsDbContext khong phai tham chieu project nao.
        foreach (var moduleAssembly in _modules.Items)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(moduleAssembly);
        }
    }
}
