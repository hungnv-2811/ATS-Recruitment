namespace ATS.AiScreening.Domain.Ports;

/// <summary>
/// An danh CV. Port NAY VA ban hien thuc cua no deu nam trong AiScreening.Domain.
/// </summary>
/// <remarks>
/// Dat o Recruitment.Domain thi module do phai tham chieu <see cref="AnonymizedCv"/>
/// cua module khac (vi pham ranh gioi module), va constructor <c>internal</c> se
/// khong bien dich duoc qua ranh gioi assembly. Xem ADR-3.
/// </remarks>
public interface IAnonymizer
{
    Task<AnonymizedCv> AnonymizeAsync(string rawCvText, CancellationToken ct = default);
}
