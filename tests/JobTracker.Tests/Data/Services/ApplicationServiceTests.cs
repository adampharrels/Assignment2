using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Interfaces;
using JobTracker.Data;
using JobTracker.Data.Services;
using JobTracker.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Tests.Data.Services;

[TestFixture]
public class ApplicationServiceTests
{
    private TestDatabase _database = null!;
    private ApplicationService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(_database);
        _service = new ApplicationService(_database);
        await RemoveSeedApplicationsAsync();
    }

    /// <summary>Starts from an empty applications table: the initializer now saves the G1 demo applications.</summary>
    private async Task RemoveSeedApplicationsAsync()
    {
        await using JobTrackerDbContext context = _database.CreateDbContext();
        context.Applications.RemoveRange(await context.Applications.Include(application => application.StageHistory).ToListAsync());
        await context.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _database.Dispose();
    }

    [Test]
    public async Task CreateAsync_NewCompany_CreatesCompanyAndFirstStageEvent()
    {
        JobApplication created = await _service.CreateAsync(NewApplication(), "Brand New Pty Ltd");

        JobApplication? loaded = await _service.GetByIdAsync(created.Id);

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Company!.Name, Is.EqualTo("Brand New Pty Ltd"));
            Assert.That(loaded.StageHistory, Has.Count.EqualTo(1));
            Assert.That(loaded.StageHistory[0].FromStatus, Is.Null);
            Assert.That(loaded.StageHistory[0].ToStatus, Is.EqualTo(ApplicationStatus.Applied));
        });
    }

    [Test]
    public async Task CreateAsync_ExistingCompanyWithDifferentCase_ReusesTheCompany()
    {
        await _service.CreateAsync(NewApplication(), "Canva");
        await _service.CreateAsync(NewApplication(), "  canva ");

        await using JobTrackerDbContext context = _database.CreateDbContext();
        Assert.That(await context.Companies.CountAsync(company => company.Name == "Canva"), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateAsync_SalaryMinAboveMax_ThrowsAndSavesNothing()
    {
        JobApplication application = NewApplication();
        application.SalaryMin = 120_000;
        application.SalaryMax = 90_000;

        Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(application, "Canva"));
        Assert.That(await _service.GetAllAsync(), Is.Empty);
    }

    [Test]
    public void CreateAsync_EmptyCompanyName_Throws()
    {
        var exception = Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(NewApplication(), "   "));
        Assert.That(exception!.Message, Does.Contain("Company"));
    }

    [Test]
    public async Task UpdateAsync_ChangesDetailsButNotStatus()
    {
        JobApplication created = await _service.CreateAsync(NewApplication(), "Canva");
        JobApplication edited = (await _service.GetByIdAsync(created.Id))!;
        edited.RoleTitle = "Graduate Data Analyst";
        edited.Status = ApplicationStatus.Offer;

        await _service.UpdateAsync(edited, "Atlassian");

        JobApplication reloaded = (await _service.GetByIdAsync(created.Id))!;
        Assert.Multiple(() =>
        {
            Assert.That(reloaded.RoleTitle, Is.EqualTo("Graduate Data Analyst"));
            Assert.That(reloaded.Company!.Name, Is.EqualTo("Atlassian"));
            Assert.That(reloaded.Status, Is.EqualTo(ApplicationStatus.Applied));
        });
    }

    [Test]
    public void UpdateAsync_MissingApplication_ThrowsKeyNotFound()
    {
        JobApplication missing = NewApplication();
        missing.Id = 999;

        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(missing, "Canva"));
    }

    [Test]
    public async Task DeleteAsync_RemovesTheApplicationItsHistoryAndInterviews()
    {
        JobApplication created = await _service.CreateAsync(NewApplication(), "Canva");
        await using (JobTrackerDbContext context = _database.CreateDbContext())
        {
            context.Interviews.Add(new Interview { ApplicationId = created.Id, ScheduledAt = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc) });
            await context.SaveChangesAsync();
        }

        await _service.DeleteAsync(created.Id);

        await using JobTrackerDbContext check = _database.CreateDbContext();
        Assert.Multiple(async () =>
        {
            Assert.That(await check.Applications.AnyAsync(), Is.False);
            Assert.That(await check.StageEvents.AnyAsync(), Is.False);
            Assert.That(await check.Interviews.AnyAsync(interview => interview.ApplicationId == created.Id), Is.False);
        });
    }

    [Test]
    public async Task ChangeStatusAsync_LegalMove_RecordsTheEvent()
    {
        JobApplication created = await _service.CreateAsync(NewApplication(), "Canva");

        StageEvent stageEvent = await _service.ChangeStatusAsync(created.Id, ApplicationStatus.Interview, "Recruiter called");

        JobApplication reloaded = (await _service.GetByIdAsync(created.Id))!;
        Assert.Multiple(() =>
        {
            Assert.That(stageEvent.FromStatus, Is.EqualTo(ApplicationStatus.Applied));
            Assert.That(reloaded.Status, Is.EqualTo(ApplicationStatus.Interview));
            Assert.That(reloaded.StageHistory.Select(item => item.ToStatus),
                Is.EqualTo(new[] { ApplicationStatus.Applied, ApplicationStatus.Interview }));
        });
    }

    [Test]
    public async Task ChangeStatusAsync_IllegalMove_ThrowsAndKeepsTheStatus()
    {
        JobApplication created = await _service.CreateAsync(NewApplication(), "Canva");
        await _service.ChangeStatusAsync(created.Id, ApplicationStatus.Rejected);

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.ChangeStatusAsync(created.Id, ApplicationStatus.Interview));
        Assert.That((await _service.GetByIdAsync(created.Id))!.Status, Is.EqualTo(ApplicationStatus.Rejected));
    }

    [Test]
    public async Task ChangeStatusAsync_FromWishlist_SetsTheAppliedDate()
    {
        JobApplication wishlist = NewApplication();
        wishlist.Status = ApplicationStatus.Wishlist;
        wishlist.DateApplied = null;
        JobApplication created = await _service.CreateAsync(wishlist, "Canva");

        await _service.ChangeStatusAsync(created.Id, ApplicationStatus.Applied);

        Assert.That((await _service.GetByIdAsync(created.Id))!.DateApplied, Is.EqualTo(DateTime.Today));
    }

    [Test]
    public async Task ApplicationsChanged_IsRaisedForEachChange()
    {
        var kinds = new List<ApplicationChangeKind>();
        _service.ApplicationsChanged += (_, args) => kinds.Add(args.Kind);

        JobApplication created = await _service.CreateAsync(NewApplication(), "Canva");
        await _service.ChangeStatusAsync(created.Id, ApplicationStatus.Assessment);
        await _service.DeleteAsync(created.Id);

        Assert.That(kinds, Is.EqualTo(new[] { ApplicationChangeKind.Created, ApplicationChangeKind.StatusChanged, ApplicationChangeKind.Deleted }));
    }

    [Test]
    public async Task SearchAsync_CombinesTextAndStatusFilters()
    {
        await _service.CreateAsync(NewApplication("Graduate Frontend Engineer"), "Canva");
        JobApplication backend = await _service.CreateAsync(NewApplication("Graduate Backend Engineer"), "Atlassian");
        await _service.ChangeStatusAsync(backend.Id, ApplicationStatus.Interview);

        IReadOnlyList<JobApplication> results = await _service.SearchAsync(new ApplicationFilter
        {
            SearchText = "engineer",
            Statuses = new[] { ApplicationStatus.Interview }
        });

        Assert.That(results.Select(application => application.RoleTitle), Is.EqualTo(new[] { "Graduate Backend Engineer" }));
    }

    [Test]
    public async Task GetApplicationsAsync_LeavesOutWishlistItems()
    {
        await _service.CreateAsync(NewApplication(), "Canva");
        JobApplication wishlist = NewApplication();
        wishlist.Status = ApplicationStatus.Wishlist;
        wishlist.DateApplied = null;
        await _service.CreateAsync(wishlist, "Atlassian");

        var records = await _service.GetApplicationsAsync();

        Assert.That(records.Select(record => record.CompanyName), Is.EqualTo(new[] { "Canva" }));
    }

    /// <summary>A valid, submitted application; tests change one thing at a time.</summary>
    private static JobApplication NewApplication(string role = "Graduate Software Engineer") => new JobApplication
    {
        RoleTitle = role,
        Location = "Sydney",
        WorkMode = WorkMode.Hybrid,
        Source = ApplicationSource.LinkedIn,
        Status = ApplicationStatus.Applied,
        SalaryMin = 85_000,
        SalaryMax = 95_000,
        JobDescription = "We use C#, .NET and SQL.",
        DateApplied = new DateTime(2026, 9, 1)
    };
}
