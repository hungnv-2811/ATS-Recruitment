using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace ATS.Persistence;

/// <summary>
/// Diem nap cau hinh entity cua tung module.
/// </summary>
/// <remarks>
/// Giu cho <see cref="AtsDbContext"/> KHONG phai tham chieu project cua module nao.
/// Moi module dang ky assembly cua minh luc khoi dong (Composition Root), nho vay
/// ranh gioi module van duoc giu nguyen.
/// </remarks>
public static class AtsDbContextConfigurator
{
    private static readonly List<Assembly> ModuleAssemblies = [];

    /// <summary>Goi tu Composition Root, truoc khi tao DbContext.</summary>
    public static void Register(Assembly moduleInfrastructureAssembly)
    {
        if (!ModuleAssemblies.Contains(moduleInfrastructureAssembly))
        {
            ModuleAssemblies.Add(moduleInfrastructureAssembly);
        }
    }

    internal static void ApplyModuleConfigurations(ModelBuilder modelBuilder)
    {
        foreach (var assembly in ModuleAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}
