using System.Text;
using JobTracker.Data.Profile;

namespace JobTracker.Tests.Data.Profile;

[TestFixture]
public class ResumeFileStoreTests
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n% a tiny pretend PDF\n%%EOF\n");
    private static readonly byte[] Docx = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 };

    private string _directory = null!;
    private ResumeFileStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "jobtracker-tests", Guid.NewGuid().ToString("N"));
        _store = new ResumeFileStore(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Test]
    public void Get_NothingUploaded_ReturnsNull()
    {
        Assert.That(_store.Get(), Is.Null);
        Assert.That(_store.Open(), Is.Null);
    }

    [Test]
    public async Task SaveAsync_KeepsTheFileAndItsName()
    {
        StoredResume saved = await SaveAsync("Jess Nguyen CV.pdf", Pdf);

        StoredResume? stored = _store.Get();
        Assert.Multiple(() =>
        {
            Assert.That(saved.FileName, Is.EqualTo("Jess Nguyen CV.pdf"));
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.FileName, Is.EqualTo("Jess Nguyen CV.pdf"));
            Assert.That(stored.SizeBytes, Is.EqualTo(Pdf.Length));
            Assert.That(stored.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(stored.CanOpenInBrowser, Is.True);
        });
    }

    [Test]
    public async Task Open_GivesBackTheSameBytes()
    {
        await SaveAsync("resume.pdf", Pdf);

        (Stream content, StoredResume _) = _store.Open()!.Value;
        using (content)
        using (var copy = new MemoryStream())
        {
            await content.CopyToAsync(copy);
            Assert.That(copy.ToArray(), Is.EqualTo(Pdf));
        }
    }

    [Test]
    public async Task SaveAsync_SecondUpload_ReplacesTheFirst()
    {
        await SaveAsync("old resume.pdf", Pdf);
        await SaveAsync("new resume.docx", Docx);

        Assert.That(_store.Get()!.FileName, Is.EqualTo("new resume.docx"));
        Assert.That(Directory.GetFiles(_directory), Has.Length.EqualTo(1));
    }

    [Test]
    public async Task SaveAsync_FailedUpload_KeepsTheOldResume()
    {
        await SaveAsync("resume.pdf", Pdf);

        Assert.ThrowsAsync<FormatException>(() => SaveAsync("fake.pdf", Encoding.ASCII.GetBytes("not really a pdf")));

        Assert.That(_store.Get()!.FileName, Is.EqualTo("resume.pdf"));
        Assert.That(Directory.GetFiles(_directory), Has.Length.EqualTo(1), "The temporary upload file is cleaned up.");
    }

    [TestCase("photo.png", "Upload a PDF")]
    [TestCase("resume.pdf", "real .pdf file")]
    public void SaveAsync_WrongKindOfFile_Throws(string fileName, string expected)
    {
        var exception = Assert.ThrowsAsync<FormatException>(() => SaveAsync(fileName, Docx));

        Assert.That(exception!.Message, Does.Contain(expected));
    }

    [Test]
    public void SaveAsync_EmptyFile_Throws()
    {
        var exception = Assert.ThrowsAsync<FormatException>(() => SaveAsync("resume.txt", Array.Empty<byte>()));

        Assert.That(exception!.Message, Does.Contain("empty"));
    }

    [Test]
    public void SaveAsync_FileOverTheLimit_Throws()
    {
        byte[] large = new byte[ResumeFileStore.MaxFileSize + 10];
        Pdf.CopyTo(large, 0);

        var exception = Assert.ThrowsAsync<FormatException>(() => SaveAsync("huge.pdf", large));

        Assert.That(exception!.Message, Does.Contain("larger than 5 MB"));
        Assert.That(_store.Get(), Is.Null);
    }

    [TestCase("../../etc/resume.pdf", "resume.pdf")]
    [TestCase("C:\\Users\\jess\\My CV.pdf", "My CV.pdf")]
    [TestCase("....pdf", "resume.pdf")]
    public async Task SaveAsync_FolderPartsInTheName_AreDropped(string fileName, string expected)
    {
        StoredResume saved = await SaveAsync(fileName, Pdf);

        Assert.That(saved.FileName, Is.EqualTo(expected));
        Assert.That(File.Exists(Path.Combine(_directory, expected)), Is.True);
    }

    [Test]
    public async Task Delete_RemovesTheResume()
    {
        await SaveAsync("resume.pdf", Pdf);

        _store.Delete();

        Assert.That(_store.Get(), Is.Null);
    }

    [TestCase("CV.PDF", true)]
    [TestCase("resume.docx", true)]
    [TestCase("resume.doc", true)]
    [TestCase("resume.exe", false)]
    [TestCase(null, false)]
    public void IsSupported_ChecksTheExtension(string? fileName, bool expected)
    {
        Assert.That(ResumeFileStore.IsSupported(fileName), Is.EqualTo(expected));
    }

    private async Task<StoredResume> SaveAsync(string fileName, byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return await _store.SaveAsync(stream, fileName);
    }
}
