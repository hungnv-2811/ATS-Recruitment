namespace ATS.Recruitment.Application.Ports;

/// <summary>
/// Recruitment nho AiScreening cham diem mot don ung tuyen.
/// </summary>
/// <remarks>
/// Interface dat o ben CHU DONG GOI (Recruitment), adapter hien thuc o
/// <c>AiScreening.Infrastructure.ScreeningTriggerAdapter</c>.
///
/// Chu y chi truyen <c>applicationId</c>: Recruitment KHONG biet gi ve Redis, LLM,
/// va KHONG tham chieu <c>AnonymizedCv</c>. Viec doc CV va an danh xay ra hoan toan
/// ben trong AiScreening. Xem docs/architecture.md muc 3.3.
/// </remarks>
public interface IApplicationScreeningTrigger
{
    Task RequestScreeningAsync(Guid applicationId, CancellationToken ct = default);
}
