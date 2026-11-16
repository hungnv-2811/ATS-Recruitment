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

    /// <summary>
    /// Dich loi UNIQUE cua Postgres (23505) sang <see cref="DuplicateKeyException"/> cua
    /// SharedKernel, de Application tra 409 thay vi de 500 lot ra ngoai.
    /// </summary>
    /// <remarks>
    /// Kiem "da ton tai chua" truoc khi INSERT khong du: hai request dong thoi cung qua buoc
    /// kiem. Chi rang buoc trong database moi chan duoc; o day chi doi cach bao loi.
    /// IUnitOfWork.SaveChangesAsync(ct) va SaveChangesAsync(ct) deu di qua overload nay.
    /// </remarks>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (UniqueViolation.TryGetConstraint(ex, out var constraint))
        {
            throw new DuplicateKeyException(constraint, "Ban ghi da ton tai (vi pham rang buoc duy nhat).", ex);
        }
    }
}

/// <summary>Nhan dien loi vi pham UNIQUE cua PostgreSQL — tach rieng de test duoc khong can database.</summary>
public static class UniqueViolation
{
    public static bool TryGetConstraint(DbUpdateException exception, out string? constraintName)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation } pg)
        {
            constraintName = pg.ConstraintName;
            return true;
        }

        constraintName = null;
        return false;
    }
}
