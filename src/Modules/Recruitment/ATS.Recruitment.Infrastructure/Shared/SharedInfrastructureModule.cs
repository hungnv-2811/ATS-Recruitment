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
/// API va Worker deu goi ham nay. Khong adapter nao mo socket/file luc dang ky.
/// </remarks>
public static class SharedInfrastructureModule
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Doc cau hinh ngay bay gio (khong giu IConfiguration trong lambda).
        var smtp = configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>() ?? new SmtpOptions();
        var fileRoot = configuration["FileStorage:RootPath"];

        // Email: co Smtp:Host thi gui that (MailHog khi dev), khong thi ghi log roi bo qua.
        if (UsesSmtp(smtp))
        {
            services.AddSingleton(smtp);
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, NullEmailSender>();
        }

        services.AddSingleton<IFileStorage>(new LocalFileStorage(
            string.IsNullOrWhiteSpace(fileRoot)
                ? Path.Combine(AppContext.BaseDirectory, "data", "cvs")
                : fileRoot));

        services.AddSingleton<ICvFormatTextExtractor, PdfPigTextExtractor>();
        services.AddSingleton<ICvFormatTextExtractor, OpenXmlTextExtractor>();
        services.AddSingleton<ICvTextExtractor, CompositeCvTextExtractor>();

        return services;
    }

    /// <summary>Quy tac chon adapter email — tach ra de test duoc.</summary>
    public static bool UsesSmtp(SmtpOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return !string.IsNullOrWhiteSpace(options.Host);
    }
}
