namespace ATS.AiScreening.Domain;

/// <summary>
/// Chuoi trang thai KY THUAT cua viec cham diem AI.
/// DOC LAP hoan toan voi trang thai ung tuyen (nghiep vu, do HR dieu khien) — RB9.
/// HR van chuyen trang thai ung tuyen duoc khi cot diem con trong hoac Failed.
/// </summary>
public enum ScreeningStatus
{
    Pending = 0,
    Processing = 1,
    Scored = 2,
    Failed = 3,
}
