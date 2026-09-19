namespace ATS.SharedKernel.Ports;

/// <summary>
/// Gui email. Dat o SharedKernel vi CA HAI module can:
/// Identity (dat lai mat khau) va Recruitment (moi phong van).
/// Day la ha tang ky thuat, khong mang nghiep vu — xem docs/contracts.md muc 2.4.
/// </summary>
/// <remarks>
/// Gui mail KHONG BAO GIO duoc lam hong nghiep vu: neu SMTP loi thi buoi phong van
/// van phai duoc luu. Xem UC-04 trong docs/use-cases.md.
/// </remarks>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public sealed record EmailMessage(
    string ToAddress,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);
