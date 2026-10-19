using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Interfaces;
using JobTracker.Data;
using JobTracker.Data.Services;
using Microsoft.EntityFrameworkCore;
using JobTracker.Tests.Data;

namespace JobTracker.Tests.Data.Services;

[TestFixture]
public class ApplicationSearchTests
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

        await AddAsync("Graduate Frontend Engineer", "Canva", "Sydney", WorkMode.Hybrid, ApplicationSource.LinkedIn, 85_000, 95_000, notes: "Met them at the career fair");
        await AddAsync("Graduate Data Analyst", "NAB", "Melbourne", WorkMode.OnSite, ApplicationSource.Seek, 70_000, 75_000);
        await AddAsync("Graduate Backend Engineer", "Atlassian", "Remote", WorkMode.Remote, ApplicationSource.Referral, null, 110_000);
        await AddAsync("Graduate Cloud Engineer", "Telstra", "Sydney", WorkMode.OnSite, ApplicationSource.Seek, null, null);
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
    public async Task EmptyFilter_ReturnsEverything()
    {
        var filter = new ApplicationFilter();

        Assert.That(filter.IsEmpty, Is.True);
        Assert.That(await _service.SearchAsync(filter), Has.Count.EqualTo(4));
    }

    [TestCase("canva", "Graduate Frontend Engineer")]
    [TestCase("DATA", "Graduate Data Analyst")]
    [TestCase("career fair", "Graduate Frontend Engineer")]
    public async Task SearchText_MatchesRoleCompanyOrNotes_IgnoringCase(string text, string expectedRole)
    {
        IReadOnlyList<JobApplication> results = await _service.SearchAsync(new ApplicationFilter { SearchText = text });

        Assert.That(Roles(results), Is.EqualTo(new[] { expectedRole }));
    }

    [Test]
    public async Task SourceFilter_WithTwoSources_ReturnsEitherSource()
    {
        IReadOnlyList<JobApplication> results = await _service.SearchAsync(new ApplicationFilter
        {
            Sources = new[] { ApplicationSource.LinkedIn, ApplicationSource.Referral }
        });

        Assert.That(Roles(results), Is.EquivalentTo(new[] { "Graduate Frontend Engineer", "Graduate Backend Engineer" }));
    }

    [Test]
    public async Task WorkModeAndLocation_Combine()
    {
        IReadOnlyList<JobApplication> results = await _service.SearchAsync(new ApplicationFilter
        {
            WorkModes = new[] { WorkMode.OnSite },
            Location = "sydney"
        });

        Assert.That(Roles(results), Is.EqualTo(new[] { "Graduate Cloud Engineer" }));
    }

    [Test]
    public async Task MinimumSalary_UsesTheMaximumAndLeavesOutAdsWithoutSalary()
    {
        IReadOnlyList<JobApplication> results = await _service.SearchAsync(new ApplicationFilter { MinimumSalary = 90_000 });

        Assert.That(Roles(results), Is.EquivalentTo(new[] { "Graduate Frontend Engineer", "Graduate Backend Engineer" }));
    }

    [Test]
    public async Task FiltersThatMatchNothing_ReturnAnEmptyList()
    {
        IReadOnlyList<JobApplication> results = await _service.SearchAsync(new ApplicationFilter
        {
            SearchText = "engineer",
            Sources = new[] { ApplicationSource.CareerFair }
        });

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void IsEmpty_IsFalseOnceAnyFilterIsSet()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new ApplicationFilter { SearchText = "  " }.IsEmpty, Is.True);
            Assert.That(new ApplicationFilter { Skill = "react" }.IsEmpty, Is.False);
            Assert.That(new ApplicationFilter { MinimumSalary = 1 }.IsEmpty, Is.False);
            Assert.That(new ApplicationFilter { Statuses = new[] { ApplicationStatus.Offer } }.IsEmpty, Is.False);
        });
    }

    private static IEnumerable<string> Roles(IEnumerable<JobApplication> applications) =>
        applications.Select(application => application.RoleTitle);

    private async Task AddAsync(string role, string company, string location, WorkMode workMode, ApplicationSource source,
        decimal? salaryMin, decimal? salaryMax, string? notes = null)
    {
        await _service.CreateAsync(new JobApplication
        {
            RoleTitle = role,
            Location = location,
            WorkMode = workMode,
            Source = source,
            Status = ApplicationStatus.Applied,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            Notes = notes,
            DateApplied = new DateTime(2026, 9, 1)
        }, company);
    }
}
