using System.Net;
using System.Net.Sockets;
using ATS.Recruitment.Infrastructure.Shared;
using ATS.SharedKernel.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ATS.IntegrationTests.Shared;

/// <summary>Ha tang dung chung (TV1): chon adapter email theo cau hinh, loi SMTP phai noi ra ngoai.</summary>
public sealed class EmailSenderTests
{
    private static IEmailSender Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddSharedInfrastructure(configuration);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        return provider.GetRequiredService<IEmailSender>();
    }

    [Fact]
    public void Co_Smtp_Host_thi_dung_SmtpEmailSender()
    {
        var sender = Resolve(new()
        {
            ["Smtp:Host"] = "mailhog",
            ["Smtp:Port"] = "1025",
            ["Smtp:From"] = "no-reply@ats.local",
        });

        Assert.IsType<SmtpEmailSender>(sender);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Khong_co_Smtp_Host_thi_dung_NullEmailSender(string? host)
    {
        var sender = Resolve(new() { ["Smtp:Host"] = host });

        Assert.IsType<NullEmailSender>(sender);
    }

    [Fact]
    public async Task NullEmailSender_khong_nem_loi()
    {
        var sender = new NullEmailSender(NullLogger<NullEmailSender>.Instance);

        await sender.SendAsync(new EmailMessage("a@b.c", "Tieu de", "<p>x</p>"));
    }

    [Fact]
    public async Task SmtpEmailSender_khong_ket_noi_duoc_thi_nem_exception_cho_nguoi_goi()
    {
        // Lay mot cong vua ranh roi dong lai: chac chan khong co SMTP nao nghe o do.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var sender = new SmtpEmailSender(
            new SmtpOptions { Host = "127.0.0.1", Port = port, TimeoutMs = 5_000 },
            NullLogger<SmtpEmailSender>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            sender.SendAsync(new EmailMessage("hr@ats.local", "Moi phong van", "<p>Hen</p>", "Hen")));
    }

    [Fact]
    public void SmtpEmailSender_thieu_Host_thi_bao_loi_ngay()
    {
        Assert.ThrowsAny<ArgumentException>(() => new SmtpEmailSender(
            new SmtpOptions { Host = " " },
            NullLogger<SmtpEmailSender>.Instance));
    }
}
