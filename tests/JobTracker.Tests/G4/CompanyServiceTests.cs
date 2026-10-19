using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Companies;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Data;
using JobTracker.Data.Services;
using JobTracker.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Tests.G4;

[TestFixture]
public class CompanyServiceTests
{
    private TestDatabase _database = null!;
    private CompanyService _service = null!;
    private ApplicationService _applications = null!;

    [SetUp]
    public async Task SetUp()
    {
        // The initializer adds the 15 G1 demo companies (Atlassian, Canva, ...) but no applications.
        _database = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(_database);
        _service = new CompanyService(_database);
        _applications = new ApplicationService(_database);
    }

    [TearDown]
    public void TearDown()
    {
        _database.Dispose();
    }

    [Test]
    public async Task GetAllAsync_ReturnsCompaniesInNameOrder()
    {
        IReadOnlyList<Company> companies = await _service.GetAllAsync();

        Assert.That(companies, Has.Count.EqualTo(SeedData.CreateCompanies().Count));
        Assert.That(companies.Select(company => company.Name), Is.Ordered.Using((IComparer<string>)StringComparer.OrdinalIgnoreCase));
    }

    [Test]
    public async Task CreateAsync_TrimsTextAndBlanksBecomeNull()
    {
        Company created = await _service.CreateAsync(new Company
        {
            Name = "  Fresh Start Pty Ltd ",
            Industry = "  ",
            Website = " https://www.freshstart.com.au ",
            Size = CompanySize.Small
        });

        Company? loaded = await _service.GetByIdAsync(created.Id);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Name, Is.EqualTo("Fresh Start Pty Ltd"));
            Assert.That(loaded.Industry, Is.Null);
            Assert.That(loaded.Website, Is.EqualTo("https://www.freshstart.com.au"));
            Assert.That(loaded.Size, Is.EqualTo(CompanySize.Small));
        });
    }

    [TestCase("Canva")]
    [TestCase("  canva ")]
    [TestCase("CANVA")]
    public void CreateAsync_DuplicateNameIgnoringCaseAndSpaces_Throws(string name)
    {
        var exception = Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(new Company { Name = name }));

        Assert.That(exception!.Message, Does.Contain("already exists"));
        Assert.That(exception.ValidationResult.MemberNames, Does.Contain(nameof(Company.Name)));
    }

    [Test]
    public void CreateAsync_EmptyName_Throws()
    {
        var exception = Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(new Company { Name = "   " }));
        Assert.That(exception!.Message, Does.Contain("name is required"));
    }

    [TestCase("www.example.com")]
    [TestCase("example")]
    [TestCase("https://")]
    [TestCase("ftp://files.example.com")]
    [TestCase("https://localhost")]
    public async Task CreateAsync_InvalidWebsite_ThrowsAndSavesNothing(string website)
    {
        int before = (await _service.GetAllAsync()).Count;

        Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(new Company { Name = "Bad Link Co", Website = website }));
        Assert.That(await _service.GetAllAsync(), Has.Count.EqualTo(before));
    }

    [TestCase("https://www.example.com")]
    [TestCase("http://example.com.au/careers?team=grad")]
    public void IsValidWebsite_AcceptsHttpAndHttpsAddresses(string website)
    {
        Assert.That(Company.IsValidWebsite(website), Is.True);
    }

    [Test]
    public async Task UpdateAsync_ChangesFieldsAndKeepsOwnName()
    {
        Company canva = await FindAsync("Canva");
        canva.Notes = "Design-led culture. Grad intake opens in March.";
        canva.Industry = "Design software";

        await _service.UpdateAsync(canva);

        Company? loaded = await _service.GetByIdAsync(canva.Id);
        Assert.That(loaded!.Notes, Is.EqualTo("Design-led culture. Grad intake opens in March."));
        Assert.That(loaded.Industry, Is.EqualTo("Design software"));
    }

    [Test]
    public async Task UpdateAsync_RenameToAnotherCompanysName_Throws()
    {
        Company canva = await FindAsync("Canva");
        canva.Name = "xero";

        Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(canva));
        Assert.That((await _service.GetByIdAsync(canva.Id))!.Name, Is.EqualTo("Canva"));
    }

    [Test]
    public void UpdateAsync_MissingCompany_ThrowsKeyNotFound()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(new Company { Id = 9999, Name = "Ghost Co" }));
    }

    [Test]
    public async Task DeleteAsync_CompanyWithoutApplications_RemovesIt()
    {
        Company company = await _service.CreateAsync(new Company { Name = "Short Lived Ltd" });

        await _service.DeleteAsync(company.Id);

        Assert.That(await _service.GetByIdAsync(company.Id), Is.Null);
    }

    [Test]
    public async Task DeleteAsync_CompanyWithApplications_ThrowsAndKeepsIt()
    {
        await _applications.CreateAsync(NewApplication(), "Atlassian");
        Company atlassian = await FindAsync("Atlassian");

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(atlassian.Id));

        Assert.That(exception!.Message, Does.Contain("1 application"));
        Assert.That(await _service.GetByIdAsync(atlassian.Id), Is.Not.Null);
    }

    [Test]
    public void DeleteAsync_MissingCompany_ThrowsKeyNotFound()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(9999));
    }

    [Test]
    public async Task GetOrCreateAsync_ExistingNameWithDifferentCase_ReturnsTheSameCompany()
    {
        Company canva = await FindAsync("Canva");

        Company found = await _service.GetOrCreateAsync("  cAnVa ");

        Assert.That(found.Id, Is.EqualTo(canva.Id));
        await using JobTrackerDbContext context = _database.CreateDbContext();
        Assert.That(await context.Companies.CountAsync(company => company.Name.ToLower() == "canva"), Is.EqualTo(1));
    }

    [Test]
    public async Task GetOrCreateAsync_NewName_CreatesIt()
    {
        Company created = await _service.GetOrCreateAsync(" Brand New Co ");

        Assert.That(created.Id, Is.GreaterThan(0));
        Assert.That(created.Name, Is.EqualTo("Brand New Co"));
    }

    [Test]
    public async Task GetSummariesAsync_CountsApplicationsAndFindsBestStage()
    {
        // Atlassian: one application reaches Interview and is then rejected, another is only Applied.
        JobApplication interviewed = await _applications.CreateAsync(NewApplication(), "Atlassian");
        await _applications.ChangeStatusAsync(interviewed.Id, ApplicationStatus.Interview);
        await _applications.ChangeStatusAsync(interviewed.Id, ApplicationStatus.Rejected);
        await _applications.CreateAsync(NewApplication(), "Atlassian");

        // Xero: a single wishlist item.
        await _applications.CreateAsync(new JobApplication { RoleTitle = "Graduate Engineer", Location = "Remote", Status = ApplicationStatus.Wishlist }, "Xero");

        IReadOnlyList<CompanySummary> summaries = await _service.GetSummariesAsync();
        CompanySummary atlassian = summaries.Single(summary => summary.Company.Name == "Atlassian");
        CompanySummary xero = summaries.Single(summary => summary.Company.Name == "Xero");
        CompanySummary canva = summaries.Single(summary => summary.Company.Name == "Canva");

        Assert.Multiple(() =>
        {
            Assert.That(atlassian.ApplicationCount, Is.EqualTo(2));
            Assert.That(atlassian.ActiveCount, Is.EqualTo(1), "The rejected application is no longer open.");
            Assert.That(atlassian.BestStage, Is.EqualTo(ApplicationStatus.Interview), "Rejected after an interview still counts as reaching Interview.");
            Assert.That(atlassian.LastAppliedOn, Is.EqualTo(DateTime.Today.AddDays(-3)));

            Assert.That(xero.ApplicationCount, Is.EqualTo(1));
            Assert.That(xero.BestStage, Is.EqualTo(ApplicationStatus.Wishlist));
            Assert.That(xero.LastAppliedOn, Is.Null);

            Assert.That(canva.ApplicationCount, Is.Zero);
            Assert.That(canva.Applications, Is.Empty);
            Assert.That(canva.BestStage, Is.Null);
        });
    }

    [Test]
    public async Task GetSummariesAsync_IncludesEveryCompanyInNameOrder()
    {
        await _service.CreateAsync(new Company { Name = "aardvark analytics" });

        IReadOnlyList<CompanySummary> summaries = await _service.GetSummariesAsync();

        Assert.That(summaries, Has.Count.EqualTo(SeedData.CreateCompanies().Count + 1));
        Assert.That(summaries[0].Company.Name, Is.EqualTo("aardvark analytics"));
    }

    private async Task<Company> FindAsync(string name) =>
        (await _service.GetAllAsync()).Single(company => company.Name == name);

    private static JobApplication NewApplication() => new JobApplication
    {
        RoleTitle = "Graduate Software Engineer",
        Location = "Sydney",
        Status = ApplicationStatus.Applied,
        DateApplied = DateTime.Today.AddDays(-3)
    };
}
