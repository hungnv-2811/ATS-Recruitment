namespace ATS.AiScreening.Domain.Ports;

/// <summary>
/// Cham do phu hop CV ↔ JD. Dung boi CA HAI phia: HR (sang loc lo) va Ung vien (preview).
/// </summary>
/// <remarks>
/// RANG BUOC KIEN TRUC (ArchitectureTests ep): tham so CV phai la
/// <see cref="AnonymizedCv"/>, KHONG duoc la <c>string</c> hay CV tho.
/// Compiler ep RB3 thay vi trong cho ky luat lap trinh. Xem ADR-3.
/// </remarks>
public interface IAiScoringService
{
    Task<ScreeningResult> ScoreAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default);
}

