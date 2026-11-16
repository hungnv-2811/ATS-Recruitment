using ATS.Identity.Application.Ports;
using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Application.Ports;
using ATS.Recruitment.Application.Services;
using ATS.Recruitment.Domain.Ports;
using ATS.Recruitment.Infrastructure.CrossModule;
using ATS.Recruitment.Infrastructure.Persistence;
using ATS.Recruitment.Infrastructure.Queries;
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

        services.TryAddSingleton(TimeProvider.System);

        // Repository (AtsDbContext dung chung, IUnitOfWork da duoc AddAtsPersistence dang ky).
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<IHrProfileRepository, HrProfileRepository>();
        services.AddScoped<ICvRepository, CvRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IInterviewRepository, InterviewRepository>();
        services.AddScoped<IEvaluationRepository, EvaluationRepository>();

        // Application service cho ATS.Api.
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<ICvService, CvService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IInterviewService, InterviewService>();

        // Cua doc cho AiScreening (contracts.md 8.2).
        services.AddScoped<IRecruitmentQueries, RecruitmentQueries>();

        // Cau noi voi Identity (chi o tang Infrastructure).
        services.AddScoped<IUserProfileProvisioner, RecruitmentProfileProvisioner>();
        services.AddScoped<IUserDirectory, IdentityUserDirectory>();

        return services;
    }
}
