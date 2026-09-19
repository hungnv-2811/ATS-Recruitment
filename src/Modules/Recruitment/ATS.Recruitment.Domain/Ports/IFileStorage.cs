namespace ATS.Recruitment.Domain.Ports;

/// <summary>Luu file CV. Adapter phase 1: LocalFileStorage ghi vao volume ./data/cvs/.</summary>
public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default);

    Task<Stream> ReadAsync(string path, CancellationToken ct = default);

    Task DeleteAsync(string path, CancellationToken ct = default);
}
