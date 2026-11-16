using System.Globalization;

namespace ATS.Web.Services;

/// <summary>Chuoi hien thi tieng Viet cho cac gia tri enum ma API tra ve.</summary>
public static class Labels
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    /// <summary>Thu tu chuoi trang thai nghiep vu (architecture.md muc 4.1), Rejected tach rieng.</summary>
    public static readonly string[] ApplicationFlow = ["Applied", "Screening", "Interview", "Hired"];

    public static readonly string[] EmploymentTypes = ["FullTime", "PartTime", "Internship", "Contract"];

    public static string ApplicationStatus(string? status) => status switch
    {
        "Applied" => "Đã nộp",
        "Screening" => "Sàng lọc",
        "Interview" => "Phỏng vấn",
        "Hired" => "Nhận",
        "Rejected" => "Từ chối",
        _ => status ?? "—",
    };

    public static string JobStatus(string? status) => status switch
    {
        "Draft" => "Nháp",
        "Open" => "Đang mở",
        "Closed" => "Đã đóng",
        _ => status ?? "—",
    };

    public static string EmploymentType(string? type) => type switch
    {
        "FullTime" => "Toàn thời gian",
        "PartTime" => "Bán thời gian",
        "Internship" => "Thực tập",
        "Contract" => "Hợp đồng",
        _ => type ?? "—",
    };

    public static string ScreeningStatus(string? status) => status switch
    {
        "Processing" => "đang chấm",
        "Scored" => "đã chấm",
        "Failed" => "không chấm được",
        _ => "chưa chấm",
    };

    public static string InterviewMode(string? mode) => mode switch
    {
        "online" => "Online",
        "onsite" => "Tại văn phòng",
        _ => mode ?? "—",
    };

    public static string InterviewStatus(string? status) => status switch
    {
        "Scheduled" => "Đã hẹn",
        "Done" => "Đã phỏng vấn",
        "Cancelled" => "Đã huỷ",
        _ => status ?? "—",
    };

    public static string Recommendation(string? value) => value switch
    {
        "Hire" => "Nhận",
        "Consider" => "Cân nhắc",
        "Reject" => "Từ chối",
        _ => value ?? "—",
    };

    public static string QuestionCategory(string? category) => category?.ToLowerInvariant() switch
    {
        "technical" => "Chuyên môn",
        "behavioral" => "Hành vi",
        "situational" => "Tình huống",
        _ => category ?? "Khác",
    };

    public static string Role(string? role) => role switch
    {
        Roles.Candidate => "Ứng viên",
        Roles.Hr => "Nhà tuyển dụng",
        Roles.Admin => "Quản trị viên",
        _ => role ?? "—",
    };

    public static string Date(DateTimeOffset? value)
        => value is null ? "—" : value.Value.ToLocalTime().ToString("dd/MM/yyyy", Vi);

    public static string DateTime(DateTimeOffset? value)
        => value is null ? "—" : value.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Vi);

    public static string Percent(double ratio) => ratio.ToString("P0", Vi);

    /// <summary>Cac trang thai HR co the chuyen toi tu trang thai hien tai — khop luat o contracts.md 8.3.</summary>
    public static IReadOnlyList<string> NextStatuses(string? current) => current switch
    {
        "Applied" => ["Screening", "Rejected"],
        "Screening" => ["Interview", "Rejected"],
        "Interview" => ["Hired", "Rejected"],
        _ => [],
    };
}
