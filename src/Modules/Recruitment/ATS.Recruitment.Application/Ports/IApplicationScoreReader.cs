namespace ATS.Recruitment.Application.Ports;

/// <summary>
/// Recruitment hoi AiScreening "don nay dang co diem AI gi" de dung danh sach xep hang cho HR
/// va lich su ung tuyen cho ung vien.
/// </summary>
/// <remarks>
/// Cung kieu voi <see cref="IApplicationScreeningTrigger"/>: interface o ben chu dong goi
/// (Recruitment), adapter o AiScreening.Infrastructure. Chi tra kieu nguyen thuy — Recruitment
/// khong biet <c>Score</c>, <c>ScreeningResult</c> hay <c>AnonymizedCv</c> (quy tac 4).
/// Don khong co trong ket qua = chua cham (Pending) — van phai hien (RB9).
/// </remarks>
public interface IApplicationScoreReader
{
    Task<IReadOnlyDictionary<Guid, ApplicationAiScore>> GetScoresAsync(
        IReadOnlyCollection<Guid> applicationIds,
        CancellationToken ct = default);
}

/// <param name="Score">0–100, null khi chua cham hoac Failed.</param>
/// <param name="AdapterUsed">"OpenAI" | "Embedding" | "Keyword" | "Fake" — BAT BUOC hien canh diem.</param>
/// <param name="ScreeningStatus">"Pending" | "Processing" | "Scored" | "Failed".</param>
public sealed record ApplicationAiScore(
    int? Score,
    string? Summary,
    string? AdapterUsed,
    string ScreeningStatus);
