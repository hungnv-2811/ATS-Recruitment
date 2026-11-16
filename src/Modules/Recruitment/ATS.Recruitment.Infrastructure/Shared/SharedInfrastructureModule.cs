using ATS.Recruitment.Domain.Ports;
using ATS.SharedKernel.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ATS.Recruitment.Infrastructure.Shared;

/// <summary>
/// Ha tang ky thuat dung chung: email, luu file CV, trich van ban CV. — TV1 so huu.
/// </summary>
/// <remarks>
/// Dat trong Recruitment.Infrastructure theo docs/contracts.md muc 6 (SmtpEmailSender o day);
/// Identity chi thay IEmailSender qua DI, khong tham chieu project nay.
/// KHUNG TUAN 3: dang ky ban toi thieu de API khoi dong duoc. TV1 thay bang adapter that.
/// </remarks>
public static class SharedInfrastructureModule
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IEmailSender, NullEmailSender>();
        services.AddSingleton<IFileStorage>(new LocalFileStorage(
            configuration["FileStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "data", "cvs")));
        services.AddSingleton<ICvTextExtractor, PlaceholderCvTextExtractor>();

        return services;
    }
}
