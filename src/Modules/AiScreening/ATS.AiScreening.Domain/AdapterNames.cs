namespace ATS.AiScreening.Domain;

/// <summary>
/// Ten cac tang trong pipeline fallback. LUON phai hien thi ra cho nguoi dung:
/// ba tang cho ba THANG DIEM khac nhau, giau nguon di la de HR so sanh nham
/// hai dai luong khong cung don vi. Xem docs/ai-integration.md muc 5.
/// </summary>
public static class AdapterNames
{
    public const string OpenAi = "OpenAI";
    public const string Embedding = "Embedding";
    public const string Keyword = "Keyword";
    public const string Fake = "Fake";
    public const string Template = "Template";
}
