namespace ATS.Recruitment.Application.Ports;

/// <summary>
/// Recruitment hoi "email dang nhap cua user nay la gi" (gui loi moi phong van, chi tiet don cho HR).
/// </summary>
/// <remarks>
/// Email chi nam o identity.users; Recruitment khong sao chep sang candidates de khoi phai dong
/// bo. Adapter o Recruitment.Infrastructure goi port cua Identity — Domain/Application cua hai
/// module khong biet nhau.
/// </remarks>
public interface IUserDirectory
{
    Task<string?> GetEmailAsync(Guid userId, CancellationToken ct = default);
}
