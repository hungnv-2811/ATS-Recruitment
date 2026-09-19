namespace ATS.SharedKernel;

/// <summary>Loi nghiep vu — co ma de API ánh xạ sang HTTP status.</summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string message) => new("not_found", message);

    public static Error Validation(string message) => new("validation", message);

    public static Error Conflict(string message) => new("conflict", message);

    public static Error Forbidden(string message) => new("forbidden", message);

    public static Error QuotaExceeded(string message) => new("quota_exceeded", message);

    public static Error External(string message) => new("external_service", message);
}
