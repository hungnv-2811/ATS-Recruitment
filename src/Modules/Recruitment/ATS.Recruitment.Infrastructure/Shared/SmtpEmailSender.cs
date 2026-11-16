using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using ATS.SharedKernel.Ports;
using Microsoft.Extensions.Logging;

namespace ATS.Recruitment.Infrastructure.Shared;

/// <summary>Gui email qua SMTP (MailHog khi dev, SMTP that khi deploy).</summary>
/// <remarks>
/// <para>
/// Moi lan gui tao <see cref="SmtpClient"/> moi: constructor KHONG mo socket, nen dang ky
/// adapter nay khong lam API/test phai co SMTP that luc khoi dong.
/// </para>
/// <para>
/// Loi gui KHONG bi nuot o day: exception (thuong la <see cref="SmtpException"/>) duoc ghi log
/// roi nem tiep de NGUOI GOI quyet dinh — vi du UC-04 bat lai va dat <c>EmailSent=false</c>,
/// con "gui lai loi moi" thi tra 502. Nuot loi o day thi nguoi goi khong bao gio biet mail hong.
/// </para>
/// <para>Khong ghi dia chi nguoi nhan vao log (RB3 — PII).</para>
/// </remarks>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(SmtpOptions options, ILogger<SmtpEmailSender> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Host, "Smtp:Host");
        ArgumentException.ThrowIfNullOrWhiteSpace(options.From, "Smtp:From");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Port, "Smtp:Port");

        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.ToAddress, nameof(message));

        using var mail = BuildMessage(message);
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = _options.TimeoutMs,
        };

        if (!string.IsNullOrWhiteSpace(_options.User))
        {
            client.Credentials = new NetworkCredential(_options.User, _options.Password);
        }

        try
        {
            await client.SendMailAsync(mail, ct).ConfigureAwait(false);
            _logger.LogInformation(
                "Da gui email \"{Subject}\" qua SMTP {Host}:{Port}.",
                message.Subject, _options.Host, _options.Port);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Gui email \"{Subject}\" qua SMTP {Host}:{Port} that bai. Nguoi goi tu quyet dinh xu ly.",
                message.Subject, _options.Host, _options.Port);
            throw;
        }
    }

    private MailMessage BuildMessage(EmailMessage message)
    {
        var mail = new MailMessage(_options.From, message.ToAddress)
        {
            Subject = message.Subject,
            SubjectEncoding = System.Text.Encoding.UTF8,
            BodyEncoding = System.Text.Encoding.UTF8,
        };

        try
        {
            if (message.PlainTextBody is null)
            {
                mail.Body = message.HtmlBody;
                mail.IsBodyHtml = true;
            }
            else
            {
                // multipart/alternative: client khong hien HTML thi doc ban chu thuong.
                mail.Body = message.PlainTextBody;
                mail.IsBodyHtml = false;
                mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                    message.HtmlBody, System.Text.Encoding.UTF8, MediaTypeNames.Text.Html));
            }

            return mail;
        }
        catch
        {
            mail.Dispose();
            throw;
        }
    }
}
