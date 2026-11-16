using ATS.Identity.Application;
using ATS.Identity.Application.Contracts;
using ATS.Identity.Domain.Ports;
using ATS.Identity.Infrastructure.Persistence;
using ATS.Identity.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ATS.Identity.Infrastructure;

/// <summary>Dang ky module Identity (User, JWT, quen mat khau). — TV2 so huu.</summary>
/// <remarks>
/// <c>IUserProfileProvisioner</c> (tao ho so Candidate/HR luc dang ky) do module Recruitment
/// dang ky — xem <c>AddRecruitmentModule</c>.
/// </remarks>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(new AuthSettings
        {
            WebBaseUrl = string.IsNullOrWhiteSpace(configuration["Web:BaseUrl"])
                ? "http://localhost:8081"
                : configuration["Web:BaseUrl"]!,
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddSingleton<IPasswordHasher>(new BCryptPasswordHasher());
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAdminService, UserAdminService>();

        // Admin dau tien: chi khi DB da duoc migrate luc khoi dong VA co cau hinh day du.
        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];
        if (configuration.GetValue("Database:AutoMigrate", false)
            && !string.IsNullOrWhiteSpace(adminEmail)
            && !string.IsNullOrWhiteSpace(adminPassword))
        {
            services.AddSingleton(new AdminSeedOptions(
                adminEmail,
                adminPassword,
                configuration["Seed:AdminFullName"] ?? "Quan tri vien"));
            services.AddHostedService<AdminSeeder>();
        }

        return services;
    }
}
