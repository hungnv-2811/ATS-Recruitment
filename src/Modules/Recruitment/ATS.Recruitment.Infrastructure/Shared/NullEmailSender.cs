using ATS.SharedKernel.Ports;
using Microsoft.Extensions.Logging;

namespace ATS.Recruitment.Infrastructure.Shared;

/// <summary>Ghi log roi bo qua email — dung trong test va khi chua cau hinh <c>Smtp:Host</c>.</summary>
public sealed class NullEmailSender : IEmailSender
{
    private readonly ILogger<NullEmailSender> _logger;

    public NullEmailSender(ILogger<NullEmailSender> logger) => _logger = logger;

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Chi ghi tieu de, khong ghi nguoi nhan/noi dung (RB3 — PII).
        _logger.LogInformation(
            "Chua cau hinh Smtp:Host — bo qua email \"{Subject}\".",
            message.Subject);
        return Task.CompletedTask;
    }
}
