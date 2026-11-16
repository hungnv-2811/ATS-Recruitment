using ATS.Recruitment.Application.Ports;
using ATS.Recruitment.Domain.Ports;
using ATS.SharedKernel.Ports;

namespace ATS.Recruitment.Tests.Support;

// Fake viet tay — khong dung thu vien mock.

internal sealed class FakeClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 16, 8, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}

internal sealed class FakeEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    /// <summary>true = gia lap SMTP sap.</summary>
    public bool Fail { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (Fail)
        {
            throw new InvalidOperationException("SMTP khong ket noi duoc (gia lap).");
        }

        Sent.Add(message);
        return Task.CompletedTask;
    }
}

internal sealed class FakeFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var path = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        Files[path] = buffer.ToArray();
        return path;
    }

    public Task<Stream> ReadAsync(string path, CancellationToken ct = default)
        => Files.TryGetValue(path, out var bytes)
            ? Task.FromResult<Stream>(new MemoryStream(bytes))
            : throw new FileNotFoundException(path);

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        Files.Remove(path);
        return Task.CompletedTask;
    }
}

internal sealed class FakeTextExtractor : ICvTextExtractor
{
    public string Text { get; set; } = "C# .NET PostgreSQL 3 nam kinh nghiem";

    public Task<string> ExtractAsync(Stream content, string fileType, CancellationToken ct = default)
        => Task.FromResult(Text);
}

internal sealed class RecordingScreeningTrigger : IApplicationScreeningTrigger
{
    public List<Guid> Requested { get; } = [];

    public bool Fail { get; set; }

    public Task RequestScreeningAsync(Guid applicationId, CancellationToken ct = default)
    {
        if (Fail)
        {
            throw new InvalidOperationException("Redis sap (gia lap).");
        }

        Requested.Add(applicationId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeScoreReader : IApplicationScoreReader
{
    public Dictionary<Guid, ApplicationAiScore> Scores { get; } = [];

    public Task<IReadOnlyDictionary<Guid, ApplicationAiScore>> GetScoresAsync(
        IReadOnlyCollection<Guid> applicationIds,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<Guid, ApplicationAiScore>>(
            Scores.Where(kv => applicationIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
}
