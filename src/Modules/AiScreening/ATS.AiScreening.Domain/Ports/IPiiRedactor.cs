namespace ATS.AiScreening.Domain.Ports;

/// <summary>
/// Port CAP THAP cho anonymizer manh hon bang LLM (phase 2).
/// Chi lam viec tren <c>string</c> — adapter ben Infrastructure xu ly duoc text
/// nhung KHONG BAO GIO cam duoc quyen tao <see cref="AnonymizedCv"/>.
/// </summary>
/// <remarks>
/// Day la cach them LlmAnonymizer ma khong pha bao dam compile-time.
/// Cach SAI la viet <c>LlmAnonymizer : IAnonymizer</c> o Infrastructure — lam vay
/// buoc phai mo InternalsVisibleTo. Xem docs/ai-integration.md muc 3.
/// </remarks>
public interface IPiiRedactor
{
    Task<string> RedactAsync(string rawCvText, CancellationToken ct = default);
}
