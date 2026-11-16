using ATS.Recruitment.Domain.Ports;

namespace ATS.Recruitment.Infrastructure.Shared;

/// <summary>Luu CV vao thu muc cuc bo (volume <c>cv-files</c> → <c>/app/data/cvs</c> trong compose).</summary>
/// <remarks>
/// <para>
/// Ten file luu tru LUON do he thong sinh (<c>Guid v7 + duoi file</c>): ten goc cua nguoi dung
/// chi dong gop phan duoi (da loc), nen khong the dung ten file de ghi de hay thoat ra ngoai root.
/// </para>
/// <para>
/// <see cref="ReadAsync"/>/<see cref="DeleteAsync"/> nhan duong dan tu database — van kiem lai:
/// duong dan tuyet doi, <c>..</c> hay bat ky thu gi resolve ra ngoai root deu bi tu choi bang
/// <see cref="UnauthorizedAccessException"/> (chong path traversal).
/// </para>
/// </remarks>
public sealed class LocalFileStorage : IFileStorage
{
    private const int MaxExtensionLength = 10;

    private readonly string _root;

    public LocalFileStorage(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        // Chuan hoa mot lan, luon ket thuc bang dau phan cach: so sanh tien to "/data/cvs/"
        // thay vi "/data/cvs" de "/data/cvs-khac/..." khong lot qua.
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
    }

    /// <summary>Thu muc goc (tuyet doi, ket thuc bang dau phan cach).</summary>
    public string RootPath => _root;

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        Directory.CreateDirectory(_root);
        var relative = $"{Guid.CreateVersion7():N}{SafeExtension(fileName)}";
        var fullPath = Path.Combine(_root, relative);

        // CreateNew: khong bao gio ghi de file da co.
        var file = new FileStream(
            fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true);
        try
        {
            await using (file.ConfigureAwait(false))
            {
                await content.CopyToAsync(file, ct).ConfigureAwait(false);
            }
        }
        catch
        {
            // Khong de lai file do dang khi copy loi / bi huy.
            TryDelete(fullPath);
            throw;
        }

        return relative;
    }

    public Task<Stream> ReadAsync(string path, CancellationToken ct = default)
    {
        var fullPath = ResolveInsideRoot(path);
        Stream stream = new FileStream(
            fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var fullPath = ResolveInsideRoot(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    /// <summary>Doi duong dan tuong doi thanh tuyet doi, tu choi neu nam ngoai root.</summary>
    private string ResolveInsideRoot(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Path.IsPathRooted(path) || path.Contains('\0', StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Duong dan file phai la duong dan tuong doi trong thu muc luu tru.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(_root, path));
        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!fullPath.StartsWith(_root, comparison) || fullPath.Length == _root.Length)
        {
            throw new UnauthorizedAccessException("Duong dan file nam ngoai thu muc luu tru.");
        }

        return fullPath;
    }

    /// <summary>Chi giu duoi file dang ".abc123" (chu thuong, toi da 10 ky tu); con lai bo.</summary>
    private static string SafeExtension(string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        if (extension.Length is < 2 or > MaxExtensionLength + 1)
        {
            return string.Empty;
        }

        return extension.AsSpan(1).ContainsAnyExcept("abcdefghijklmnopqrstuvwxyz0123456789")
            ? string.Empty
            : extension;
    }

    private static void TryDelete(string fullPath)
    {
        try
        {
            File.Delete(fullPath);
        }
        catch (IOException)
        {
            // Bo qua: loi goc quan trong hon.
        }
        catch (UnauthorizedAccessException)
        {
            // Bo qua: loi goc quan trong hon.
        }
    }
}
