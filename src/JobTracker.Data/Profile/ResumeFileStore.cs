using System.Text;

namespace JobTracker.Data.Profile;

/// <summary>The resume file the user has uploaded.</summary>
/// <param name="FileName">The file's name as uploaded (made safe for the file system), e.g. "Jess Nguyen CV.pdf".</param>
/// <param name="SizeBytes">The file's size in bytes.</param>
/// <param name="UploadedAt">When it was uploaded (local time).</param>
public record StoredResume(string FileName, long SizeBytes, DateTime UploadedAt)
{
    /// <summary>The MIME type to send the file with, based on its extension.</summary>
    public string ContentType => Path.GetExtension(FileName).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".doc" => "application/msword",
        ".odt" => "application/vnd.oasis.opendocument.text",
        ".rtf" => "application/rtf",
        _ => "text/plain"
    };

    /// <summary>True for PDFs, which browsers can show in a tab; other types are downloaded.</summary>
    public bool CanOpenInBrowser => ContentType == "application/pdf";
}

/// <summary>
/// Keeps the user's uploaded resume file in a folder on disk (G6). There is one user, so the folder holds
/// at most one file; uploading another replaces it.
/// </summary>
/// <remarks>
/// Files are kept on disk rather than in the database so the agreed entities and schema (K1) don't change.
/// The folder is App_Data/resume next to the app, which .gitignore leaves out so nobody commits their resume.
/// </remarks>
public class ResumeFileStore
{
    /// <summary>The largest file accepted.</summary>
    public const long MaxFileSize = 5 * 1024 * 1024;

    /// <summary>The longest file name kept; longer names are shortened (keeping the extension).</summary>
    private const int MaxFileNameLength = 100;

    /// <summary>The name an upload is written to before it replaces the old file, so a failed upload keeps the old one.</summary>
    private const string TempFileName = ".upload.tmp";

    /// <summary>The file types that can be uploaded.</summary>
    public static readonly IReadOnlyList<string> Extensions = new[] { ".pdf", ".docx", ".doc", ".odt", ".rtf", ".txt" };

    private readonly string _directory;

    /// <summary>Creates the store. The folder is created on first upload.</summary>
    /// <param name="directory">The folder to keep the resume in.</param>
    public ResumeFileStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
    }

    /// <summary>The extensions as an HTML accept attribute, e.g. ".pdf,.docx,...".</summary>
    public static string AcceptAttribute => string.Join(",", Extensions);

    /// <summary>True when the file name has an extension that can be uploaded (ignoring case).</summary>
    public static bool IsSupported(string? fileName) =>
        Extensions.Contains(Path.GetExtension(fileName ?? string.Empty), StringComparer.OrdinalIgnoreCase);

    /// <summary>Details of the stored resume, or null when none has been uploaded.</summary>
    public StoredResume? Get()
    {
        FileInfo? file = FindFile();
        return file == null ? null : new StoredResume(file.Name, file.Length, file.LastWriteTime);
    }

    /// <summary>Opens the stored resume for reading, or returns null when there is none. The caller disposes the stream.</summary>
    public (Stream Content, StoredResume Resume)? Open()
    {
        FileInfo? file = FindFile();
        return file == null
            ? null
            : (file.OpenRead(), new StoredResume(file.Name, file.Length, file.LastWriteTime));
    }

    /// <summary>Saves an uploaded resume, replacing any earlier one.</summary>
    /// <param name="content">The file's contents.</param>
    /// <param name="fileName">The uploaded file's name; its extension must be supported.</param>
    /// <param name="cancellationToken">Cancels the upload; the old resume is kept.</param>
    /// <returns>Details of the saved file.</returns>
    /// <exception cref="FormatException">
    /// The type isn't supported, the file is empty or too large, or its contents don't match its extension.
    /// The message can be shown to the user.
    /// </exception>
    public async Task<StoredResume> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        string extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        if (!IsSupported(fileName))
        {
            throw new FormatException("Upload a PDF, Word (.docx or .doc), OpenDocument (.odt), RTF or text file.");
        }

        Directory.CreateDirectory(_directory);
        string tempPath = Path.Combine(_directory, TempFileName);
        try
        {
            // Copy at most one byte over the limit, so a huge upload is caught without reading all of it.
            await using (FileStream temp = File.Create(tempPath))
            {
                await CopyWithLimitAsync(content, temp, MaxFileSize + 1, cancellationToken);
            }

            var written = new FileInfo(tempPath);
            if (written.Length == 0)
            {
                throw new FormatException("The file is empty.");
            }

            if (written.Length > MaxFileSize)
            {
                throw new FormatException($"The file is larger than {MaxFileSize / 1024 / 1024} MB.");
            }

            if (!await LooksLikeAsync(tempPath, extension, cancellationToken))
            {
                throw new FormatException($"This doesn't look like a real {extension} file. Check it opens, then upload it again.");
            }

            // Only now remove the old resume and put the new one in its place.
            foreach (FileInfo old in StoredFiles())
            {
                old.Delete();
            }

            string finalPath = Path.Combine(_directory, SafeFileName(fileName!, extension));
            File.Move(tempPath, finalPath);
            var saved = new FileInfo(finalPath);
            return new StoredResume(saved.Name, saved.Length, saved.LastWriteTime);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>Removes the stored resume, if there is one.</summary>
    public void Delete()
    {
        foreach (FileInfo file in StoredFiles())
        {
            file.Delete();
        }
    }

    /// <summary>The stored resume file, or null.</summary>
    private FileInfo? FindFile() => StoredFiles().OrderByDescending(file => file.LastWriteTimeUtc).FirstOrDefault();

    /// <summary>Every file in the folder except an upload in progress.</summary>
    private IEnumerable<FileInfo> StoredFiles() =>
        Directory.Exists(_directory)
            ? new DirectoryInfo(_directory).GetFiles().Where(file => file.Name != TempFileName)
            : Enumerable.Empty<FileInfo>();

    /// <summary>Copies up to <paramref name="limit"/> bytes.</summary>
    private static async Task CopyWithLimitAsync(Stream source, Stream target, long limit, CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while (total < limit && (read = await source.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, limit - total)), cancellationToken)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            total += read;
        }
    }

    /// <summary>
    /// Checks the first bytes match the extension (PDFs start with "%PDF-", .docx and .odt are zips, and so on),
    /// so a renamed file of another kind is caught. Text files can hold anything.
    /// </summary>
    private static async Task<bool> LooksLikeAsync(string path, string extension, CancellationToken cancellationToken)
    {
        byte[] start = new byte[8];
        int length;
        await using (FileStream file = File.OpenRead(path))
        {
            length = await file.ReadAsync(start, cancellationToken);
        }

        bool StartsWith(params byte[] signature) => length >= signature.Length && start.AsSpan(0, signature.Length).SequenceEqual(signature);

        return extension switch
        {
            ".pdf" => StartsWith(Encoding.ASCII.GetBytes("%PDF-")),
            ".docx" or ".odt" => StartsWith(0x50, 0x4B, 0x03, 0x04),
            ".doc" => StartsWith(0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1),
            ".rtf" => StartsWith(Encoding.ASCII.GetBytes("{\\rtf")),
            _ => true
        };
    }

    /// <summary>The uploaded name without any folder part or characters the file system can't hold, at most 100 characters.</summary>
    private static string SafeFileName(string fileName, string extension)
    {
        string name = Path.GetFileNameWithoutExtension(Path.GetFileName(fileName.Replace('\\', '/')));
        char[] invalid = Path.GetInvalidFileNameChars();
        name = new string(name.Select(character => invalid.Contains(character) || char.IsControl(character) ? '_' : character).ToArray()).Trim(' ', '.');

        if (name.Length == 0)
        {
            name = "resume";
        }

        int maxNameLength = MaxFileNameLength - extension.Length;
        return (name.Length > maxNameLength ? name[..maxNameLength] : name) + extension;
    }
}
