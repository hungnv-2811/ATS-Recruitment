using ATS.Recruitment.Domain.Ports;
using ATS.SharedKernel.Ports;

namespace ATS.Recruitment.Infrastructure.Shared;

// KHUNG TUAN 3 — TV1 thay ca file nay bang adapter that (SmtpEmailSender, LocalFileStorage
// an toan duong dan, PdfPigTextExtractor, OpenXmlTextExtractor).

/// <summary>Bo qua email — dung trong test va khi chua cau hinh SMTP.</summary>
public sealed class NullEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Luu CV vao thu muc cuc bo (volume ./data/cvs trong compose).</summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string root) => _root = root;

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_root);
        var relative = $"{Guid.NewGuid():N}{Path.GetExtension(fileName).ToLowerInvariant()}";
        var file = File.Create(Path.Combine(_root, relative));
        await using (file.ConfigureAwait(false))
        {
            await content.CopyToAsync(file, ct).ConfigureAwait(false);
        }

        return relative;
    }

    public Task<Stream> ReadAsync(string path, CancellationToken ct = default)
        => Task.FromResult<Stream>(File.OpenRead(Path.Combine(_root, path)));

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        File.Delete(Path.Combine(_root, path));
        return Task.CompletedTask;
    }
}

/// <summary>Tra chuoi rong cho moi file.</summary>
public sealed class PlaceholderCvTextExtractor : ICvTextExtractor
{
    public Task<string> ExtractAsync(Stream content, string fileType, CancellationToken ct = default)
        => Task.FromResult(string.Empty);
}
