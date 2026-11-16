using ATS.Recruitment.Application.Ports;
using ATS.Recruitment.Infrastructure.CrossModule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ATS.Recruitment.Infrastructure;

/// <summary>Dang ky toan bo module Recruitment (repository, service). — TV2 so huu.</summary>
public static class RecruitmentModule
{
    public static IServiceCollection AddRecruitmentModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Mac dinh "khong co AiScreening": TryAdd de module AiScreening (dang ky SAU trong
        // Composition Root) ghi de bang adapter that. Nho vay moi nhanh feature tu build/test
        // duoc ma khong can nhanh cua nguoi khac.
        services.TryAddScoped<IApplicationScreeningTrigger, NoOpScreeningTrigger>();
        services.TryAddScoped<IApplicationScoreReader, NoScoreReader>();

        return services;
    }
}
