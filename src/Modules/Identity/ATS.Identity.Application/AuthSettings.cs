namespace ATS.Identity.Application;

/// <summary>Cau hinh nghiep vu cua Identity, Infrastructure doc tu IConfiguration roi dang ky singleton.</summary>
public sealed class AuthSettings
{
    /// <summary>Goc cua UI (<c>Web:BaseUrl</c>) — link dat lai mat khau tro ve day.</summary>
    public string WebBaseUrl { get; init; } = "http://localhost:8081";

    public const int MinPasswordLength = 8;
}
