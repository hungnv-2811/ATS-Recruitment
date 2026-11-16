using ATS.SharedKernel;

namespace ATS.Recruitment.Domain;

/// <summary>Mot CV cua ung vien (recruitment.cvs). Moi ung vien nhieu CV, dung MOT CV mac dinh.</summary>
public sealed class Cv : Entity<Guid>
{
    public const int NameMaxLength = 200;
    public const long MaxFileSizeBytes = 5L * 1024 * 1024;

    /// <summary>Dinh dang chap nhan, chu thuong, khong dau cham.</summary>
    public static readonly IReadOnlyList<string> AllowedFileTypes = ["pdf", "docx"];

    private Cv()
    {
    }

    private Cv(Guid id, Guid candidateId, string name, string filePath, string fileType, int fileSize, string extractedText, DateTimeOffset uploadedAt)
        : base(id)
    {
        CandidateId = candidateId;
        Name = name;
        FilePath = filePath;
        FileType = fileType;
        FileSize = fileSize;
        ExtractedText = extractedText;
        UploadedAt = uploadedAt;
    }

    public Guid CandidateId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Duong dan tuong doi do IFileStorage tra ve.</summary>
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>"pdf" | "docx".</summary>
    public string FileType { get; private set; } = string.Empty;

    /// <summary>Kich thuoc file (byte).</summary>
    public int FileSize { get; private set; }

    /// <summary>Van ban trich luc upload; rong neu khong doc duoc (van cho apply).</summary>
    public string ExtractedText { get; private set; } = string.Empty;

    public bool IsDefault { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    public bool HasText => !string.IsNullOrWhiteSpace(ExtractedText);

    public static Cv Create(Guid candidateId, string name, string filePath, string fileType, int fileSize, string? extractedText, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileType);
        return new Cv(Guid.CreateVersion7(), candidateId, name.Trim(), filePath, fileType, fileSize, extractedText ?? string.Empty, now);
    }

    /// <summary>"cv.PDF" → "pdf"; null neu khong phai pdf/docx.</summary>
    public static string? NormalizeFileType(string? fileName)
    {
        var ext = Path.GetExtension(fileName ?? string.Empty).TrimStart('.').ToLowerInvariant();
        return AllowedFileTypes.Contains(ext) ? ext : null;
    }

    public void MarkDefault() => IsDefault = true;

    public void UnmarkDefault() => IsDefault = false;
}
