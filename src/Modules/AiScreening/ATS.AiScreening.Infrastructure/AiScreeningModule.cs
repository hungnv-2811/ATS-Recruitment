using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ATS.AiScreening.Infrastructure;

/// <summary>
/// Dang ky module AiScreening (port AI, pipeline fallback, queue, cache, quota). — TV4 so huu.
/// </summary>
/// <remarks>
/// Goi SAU AddRecruitmentModule: adapter cross-module o day (ScreeningTriggerAdapter,
/// score reader) ghi de ban NoOp ma Recruitment dang ky bang TryAdd.
/// Ca API lan Worker deu goi ham nay voi CUNG cau hinh AiProvider.
/// </remarks>
public static class AiScreeningModule
{
    public static IServiceCollection AddAiScreeningModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return services;
    }
}
