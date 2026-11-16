using System.ComponentModel.DataAnnotations;

namespace ATS.Web.Models;

// Model co the sua cho EditForm. Validation o day chi de bao loi som tren form;
// API van kiem lai — khong tin du lieu tu client.

public sealed class LoginForm
{
    [Required(ErrorMessage = "Nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mật khẩu.")]
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterForm
{
    [Required(ErrorMessage = "Nhập họ tên.")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mật khẩu.")]
    [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự.")]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string Role { get; set; } = "Candidate";
}

public sealed class ForgotPasswordForm
{
    [Required(ErrorMessage = "Nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordForm
{
    [Required(ErrorMessage = "Nhập mật khẩu mới.")]
    [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class JobForm
{
    [Required(ErrorMessage = "Nhập tiêu đề.")]
    [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mô tả công việc.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập yêu cầu — AI dùng phần này để chấm điểm.")]
    public string Requirements { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập địa điểm.")]
    public string Location { get; set; } = string.Empty;

    public string EmploymentType { get; set; } = "FullTime";

    public bool Publish { get; set; } = true;

    public static JobForm From(JobDto job)
    {
        ArgumentNullException.ThrowIfNull(job);
        return new JobForm
        {
            Title = job.Title,
            Description = job.Description,
            Requirements = job.Requirements,
            Location = job.Location,
            EmploymentType = job.EmploymentType,
            Publish = job.Status == "Open",
        };
    }
}

public sealed class InterviewForm
{
    [Required(ErrorMessage = "Chọn thời gian.")]
    public DateTime? ScheduledAt { get; set; } = System.DateTime.Today.AddDays(2).AddHours(14);

    public string Mode { get; set; } = "online";

    [Required(ErrorMessage = "Nhập link họp hoặc địa điểm.")]
    public string LocationOrLink { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập người phỏng vấn.")]
    public string InterviewerName { get; set; } = string.Empty;
}

/// <summary>Cau hoi phong van dang sua tren form — AI goi y, HR quyet dinh (HR-4).</summary>
public sealed class EditableQuestion
{
    public string Category { get; set; } = "technical";

    public string Question { get; set; } = string.Empty;

    public string Source { get; set; } = "Manual";

    public static EditableQuestion From(InterviewQuestionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new EditableQuestion { Category = dto.Category, Question = dto.Question, Source = dto.Source };
    }

    public InterviewQuestionDto ToDto() => new(Category, Question.Trim(), Source);
}

public sealed class EvaluationForm
{
    [Range(1, 5, ErrorMessage = "Chọn mức độ từ 1 đến 5 sao.")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Nhập nhận xét.")]
    public string Comment { get; set; } = string.Empty;

    public string Recommendation { get; set; } = "Consider";
}
