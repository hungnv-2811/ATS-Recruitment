using System.Reflection;
using ATS.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ATS.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Cach DUY NHAT de dang ky <see cref="AtsDbContext"/>.
    /// </summary>
    /// <remarks>
    /// Bat buoc truyen danh sach assembly module ngay tai day, nen khong ton tai
    /// trang thai "da tao DbContext nhung chua dang ky module". Truoc day viec
    /// dang ky la mot loi goi rieng vao mot List static: goi thieu, hoac goi sau
    /// khi model da duoc build, deu khong bao loi — chi lam bang cua module do
    /// bien mat.
    /// </remarks>
    public static IServiceCollection AddAtsPersistence(
        this IServiceCollection services,
        string? connectionString,
        params Assembly[] moduleAssemblies)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddSingleton(new ModuleAssemblies(moduleAssemblies));
        services.AddDbContext<AtsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AtsDbContext>());

        return services;
    }
}
