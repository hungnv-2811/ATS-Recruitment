using Microsoft.Extensions.DependencyInjection;

namespace ATS.Persistence.Reporting;

/// <summary>
/// Truy van bao cao/dashboard xuyen 3 schema (tuan 9) — TV1 so huu.
/// </summary>
/// <remarks>
/// Bao cao la doc thuan va JOIN xuyen module, nen dat o ATS.Persistence (noi duy nhat
/// "thay" ca 3 schema) thay vi ep mot module doc bang cua module khac.
/// </remarks>
public static class ReportingServiceCollectionExtensions
{
    public static IServiceCollection AddAtsReporting(this IServiceCollection services) => services;
}
