namespace ATS.Recruitment.Infrastructure.Shared;

/// <summary>Cau hinh muc <c>Smtp</c> (appsettings / bien moi truong <c>Smtp__*</c>).</summary>
/// <remarks>
/// Dev: MailHog (<c>Host=mailhog</c> trong compose, <c>localhost</c> khi chay tay), cong 1025,
/// khong dang nhap, khong SSL. Bo trong <see cref="Host"/> => dung <see cref="NullEmailSender"/>.
/// </remarks>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }

    public int Port { get; set; } = 25;

    public string From { get; set; } = "no-reply@ats.local";

    public string? User { get; set; }

    public string? Password { get; set; }

    public bool EnableSsl { get; set; }

    /// <summary>Thoi gian toi da cho mot lan gui (ms). SmtpClient mac dinh 100 giay — qua lau cho request HTTP.</summary>
    public int TimeoutMs { get; set; } = 15_000;
}
