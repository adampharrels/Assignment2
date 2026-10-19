using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Data;
using JobTracker.Data.ImportExport;
using JobTracker.Data.Services;

namespace JobTracker.Tests.Data.Services;

[TestFixture]
public class ImportExportServiceTests
{
    private TestDatabase _database = null!;
    private ApplicationService _applications = null!;
    private ImportExportService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(_database);
        _applications = new ApplicationService(_database);
        _service = new ImportExportService(_applications);
    }

    [TearDown]
    public void TearDown()
    {
        _database.Dispose();
    }

    [TestCase(".csv")]
    [TestCase(".json")]
    public async Task ExportThenImportIntoAnEmptyTracker_RecreatesEveryApplication(string extension)
    {
        ApplicationFileFormat format = _service.FindFormat("backup" + extension)!;
        await _applications.CreateAsync(NewApplication("Graduate Engineer", ApplicationStatus.Interview), "Canva");
        await _applications.CreateAsync(NewApplication("Data Analyst", ApplicationStatus.Applied), "Brand New Co");
        ExportFile file = await _service.ExportAsync(format);

        // A second tracker with its own database, as if restoring a backup on another computer.
        using var otherDatabase = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(otherDatabase);
        var otherApplications = new ApplicationService(otherDatabase);
        ImportResult result = await new ImportExportService(otherApplications).ImportAsync(format, file.Content);

        IReadOnlyList<JobApplication> restored = await otherApplications.GetAllAsync();
        Assert.Multiple(() =>
        {
            Assert.That(file.FileName, Does.EndWith(extension));
            Assert.That(result.Imported, Is.EqualTo(2));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(restored.Select(application => $"{application.Company!.Name}: {application.RoleTitle} ({application.Status})"),
                Is.EquivalentTo(new[] { "Canva: Graduate Engineer (Interview)", "Brand New Co: Data Analyst (Applied)" }));
        });
    }

    [Test]
    public async Task ImportAsync_SameFileTwice_SkipsWhatIsAlreadyThere()
    {
        const string csv = "Company,Role,Location,Date applied\nCanva,Engineer,Sydney,2026-09-01\nXero,Engineer,Remote,2026-09-02";
        ApplicationFileFormat format = _service.FindFormat("a.csv")!;

        await _service.ImportAsync(format, csv);
        ImportResult second = await _service.ImportAsync(format, csv);

        Assert.That(second.Imported, Is.Zero);
        Assert.That(second.Skipped.Select(row => row.RowNumber), Is.EqualTo(new[] { 2, 3 }));
        Assert.That(await _applications.GetAllAsync(), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ImportAsync_MixedRows_ImportsGoodRowsAndReportsBadOnes()
    {
        const string csv = "Company,Role,Location,Status,Date applied\n"
            + "Canva,Engineer,Sydney,Applied,2026-09-01\n"
            + "  canva ,Analyst,Sydney,Offer,\n"
            + "Xero,Engineer,Remote,Applied,2026-09-02\n";

        ImportResult result = await _service.ImportAsync(_service.FindFormat("a.csv")!, csv);

        Assert.That(result.Imported, Is.EqualTo(2));
        Assert.That(result.Errors.Single().RowNumber, Is.EqualTo(3));
        Assert.That(result.Errors.Single().Message, Does.Contain("Applied date is required"));
        Assert.That(result.TotalRows, Is.EqualTo(3));
    }

    [Test]
    public async Task ImportAsync_ExistingCompanyInDifferentCase_IsReused()
    {
        await _service.ImportAsync(_service.FindFormat("a.csv")!, "Company,Role,Location\n  CANVA ,Engineer,Sydney");

        JobApplication imported = (await _applications.GetAllAsync()).Single();
        Assert.That(imported.Company!.Name, Is.EqualTo("Canva"), "The seeded company is matched, not duplicated.");
    }

    [Test]
    public async Task ImportAsync_UnreadableFile_ImportsNothing()
    {
        ImportResult result = await _service.ImportAsync(_service.FindFormat("a.json")!, "{ oops");

        Assert.That(result.FileError, Is.Not.Null);
        Assert.That(result.Imported, Is.Zero);
        Assert.That(await _applications.GetAllAsync(), Is.Empty);
    }

    [Test]
    public void FindFormat_UnsupportedExtension_ReturnsNull()
    {
        Assert.That(_service.FindFormat("applications.xlsx"), Is.Null);
        Assert.That(_service.FindFormat("applications.JSON"), Is.InstanceOf<JsonApplicationFormat>());
    }

    [Test]
    public void CreateTemplate_ParsesAsAFileWithNoApplications()
    {
        var csv = new CsvApplicationFormat();
        ExportFile template = _service.CreateTemplate(csv);

        Assert.That(template.FileName, Is.EqualTo("job-applications-template.csv"));
        Assert.That(csv.Parse(template.Content).FileError, Does.Contain("no applications"));
    }

    private static JobApplication NewApplication(string role, ApplicationStatus status) => new JobApplication
    {
        RoleTitle = role,
        Location = "Sydney",
        Status = status,
        DateApplied = new DateTime(2026, 9, 1),
        SalaryMin = 80_000,
        HadReferral = true
    };
}
