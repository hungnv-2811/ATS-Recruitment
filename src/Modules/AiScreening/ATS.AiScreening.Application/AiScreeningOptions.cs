namespace ATS.AiScreening.Application;

/// <summary>
/// Cau hinh AI ma Application can biet. Infrastructure doc tu IConfiguration va dang ky mot ban duy nhat.
/// </summary>
/// <remarks>
/// Lop thuan (khong IOptions) de Application khong phai keo Microsoft.Extensions.Options.
/// Khoa cau hinh: <c>AiProvider</c>, <c>OpenAI:Model</c>, <c>Ai:PreviewLimitPerDay</c>,
/// <c>Ai:BatchLimitPerDay</c>, <c>Ai:PreviewTimeoutSeconds</c>, <c>Ai:MaxApplicationsPerBatch</c>.
/// </remarks>
public sealed class AiScreeningOptions
{
    public const string ScoringPromptVersion = "scoring-v1";

    /// <summary>"Fake" | "OpenAI" — PHAI giong nhau giua api va worker.</summary>
    public string Provider { get; init; } = "Fake";

    /// <summary>Nam trong CacheKey: doi model thi cache cu khong con dung.</summary>
    public string ModelVersion { get; init; } = "fake-v1";

    public string PromptVersion { get; init; } = ScoringPromptVersion;

    public int PreviewLimitPerDay { get; init; } = 20;

    public int BatchLimitPerDay { get; init; } = 5;

    /// <summary>Tran thoi gian cho cac tang AI (OpenAI/Embedding); qua tran thi roi xuong Keyword.</summary>
    public int PreviewTimeoutSeconds { get; init; } = 30;

    public int MaxApplicationsPerBatch { get; init; } = 500;
}

/// <summary>Gia tri claim <c>role</c> — trung voi ATS.Api.Auth.Roles (Application khong tham chieu Api).</summary>
public static class CallerRoles
{
    public const string Candidate = "Candidate";
    public const string Hr = "HR";
    public const string Admin = "Admin";

    public static bool Is(string? role, string expected) => string.Equals(role, expected, StringComparison.Ordinal);
}
