using System.Reflection;

namespace ATS.Persistence;

/// <summary>
/// Danh sach assembly Infrastructure cua cac module, dung de nap
/// IEntityTypeConfiguration vao <see cref="AtsDbContext"/>.
/// </summary>
/// <remarks>
/// Day la mot THAM SO, khong phai mot registry static. Khac biet quan trong:
/// AtsDbContext khong the duoc tao ma thieu danh sach nay, nen quen cau hinh
/// se hong ngay luc khoi dong (DI khong resolve duoc) thay vi am tham sinh ra
/// mot model rong.
/// </remarks>
public sealed class ModuleAssemblies
{
    public ModuleAssemblies(params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            throw new ArgumentException(
                "Phai truyen it nhat mot assembly module, neu khong AtsDbContext se co model rong.",
                nameof(assemblies));
        }

        Items = assemblies;
    }

    public IReadOnlyList<Assembly> Items { get; }
}
