using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ATS.AiScreening.Application.Abstractions;
using ATS.AiScreening.Domain.Ports;
using ATS.Persistence;
using ATS.Recruitment.Domain.Ports;
using ATS.Recruitment.Infrastructure.Shared;
using ATS.SharedKernel.Ports;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ATS.IntegrationTests.EndToEnd;

/// <summary>
/// API THAT voi wiring THAT cua ca 3 module (Identity + Recruitment cua TV2, AiScreening cua TV4, Shared
/// cua TV1) — chi thay nhung thu can ha tang ngoai: Postgres → EF InMemory, thu muc CV → thu muc tam,
/// SMTP → ban NEM LOI (kiem UC-04 "email loi van luu buoi phong van"), Redis → hang doi trong bo nho.
/// </summary>
/// <remarks>
/// Khong stub IRecruitmentQueries, khong tu ky JWT: token lay tu response cua /api/auth/register.
/// Worker duoc gia lap bang <see cref="RunWorkerAsync"/>: rut hang doi va goi IProcessScreeningHandler
/// trong MOT scope cho MOI viec — y het vong lap cua ScreeningWorker.
/// </remarks>
public sealed class E2eApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ats-e2e-{Guid.NewGuid()}";
    private readonly string _fileRoot = Path.Combine(Path.GetTempPath(), $"ats-e2e-cvs-{Guid.NewGuid():N}");

    public ThrowingEmailSender Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.UseSetting("AiProvider", "Fake");
        builder.UseSetting("Redis:ConnectionString", string.Empty);

        builder.ConfigureTestServices(services =>
        {
            var dbDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions)
                            || d.ServiceType == typeof(AtsDbContext)
                            || (d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(AtsDbContext))))
                .ToList();
            foreach (var descriptor in dbDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AtsDbContext>(o => o.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IFileStorage>();
            services.AddSingleton<IFileStorage>(new LocalFileStorage(_fileRoot));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Email);
        });
    }

    /// <summary>Gia lap ATS.Worker: rut het hang doi, moi viec mot scope. Tra so viec da xu ly.</summary>
    public async Task<int> RunWorkerAsync()
    {
        var queue = Services.GetRequiredService<IScreeningQueue>();
        var processed = 0;
        while (await queue.DequeueAsync() is { } task)
        {
            await using var scope = Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IProcessScreeningHandler>().HandleAsync(task);
            processed++;
        }

        return processed;
    }

    /// <summary>Dang ky qua API that, tra client da gan JWT do chinh API phat.</summary>
    public async Task<(HttpClient Client, Guid UserId, string Email)> RegisterAsync(string role, string fullName)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@e2e.local";
        var anonymous = CreateClient();
        var response = await anonymous.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Matkhau@123",
            fullName,
            role,
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(role, auth.GetProperty("role").GetString());

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());
        return (client, auth.GetProperty("userId").GetGuid(), email);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (Directory.Exists(_fileRoot))
            {
                Directory.Delete(_fileRoot, recursive: true);
            }
        }
        catch (IOException)
        {
            // Thu muc tam — xoa khong duoc cung khong sao.
        }
    }
}

/// <summary>SMTP "chet": moi lan gui deu nem — giong SmtpEmailSender khi MailHog khong chay.</summary>
public sealed class ThrowingEmailSender : IEmailSender
{
    private int _attempts;

    public int Attempts => _attempts;

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _attempts);
        throw new InvalidOperationException("SMTP khong ket noi duoc (gia lap).");
    }
}

/// <summary>Sinh file DOCX that bang OpenXml — de extractor that (TV1) trich duoc chu.</summary>
public static class DocxBuilder
{
    public static byte[] Build(params string[] paragraphs)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(paragraphs.Select(p => new Paragraph(new Run(new Text(p) { Space = SpaceProcessingModeValues.Preserve })))));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    public static MultipartFormDataContent UploadForm(byte[] docx, string name)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(docx);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        form.Add(file, "file", "cv.docx");
        form.Add(new StringContent(name), "name");
        return form;
    }
}
