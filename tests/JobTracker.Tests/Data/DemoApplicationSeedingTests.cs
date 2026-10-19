using JobTracker.Core.Analytics;
using JobTracker.Data;
using JobTracker.Data.Seed;
using JobTracker.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Tests.Data;

/// <summary>
/// The G1 demo applications are stored in the database on first run, so every screen and the analytics
/// read the same applications (A7 integration).
/// </summary>
[TestFixture]
public class DemoApplicationSeedingTests
{
    private TestDatabase _database = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(_database, includeDemoApplications: true);
    }

    [TearDown]
    public void TearDown()
    {
        _database.Dispose();
    }

    [Test]
    public async Task InitializeAsync_SavesEverySeedApplicationWithItsIdAndHistory()
    {
        IReadOnlyList<SeedApplication> seeds = SeedData.CreateApplications();

        await using JobTrackerDbContext context = _database.CreateDbContext();
        var saved = await context.Applications.Include(application => application.StageHistory).ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(saved.Select(application => application.Id), Is.EquivalentTo(seeds.Select(seed => seed.Id)));
            Assert.That(saved.Sum(application => application.StageHistory.Count), Is.EqualTo(seeds.Sum(seed => seed.History.Count)));
        });
    }

    [Test]
    public async Task InitializeAsync_EverySeededInterviewPointsAtASavedApplication()
    {
        await using JobTrackerDbContext context = _database.CreateDbContext();
        var applicationIds = await context.Applications.Select(application => application.Id).ToListAsync();
        var interviewApplicationIds = await context.Interviews.Select(interview => interview.ApplicationId).Distinct().ToListAsync();

        Assert.That(interviewApplicationIds, Is.Not.Empty.And.SubsetOf(applicationIds));
    }

    [Test]
    public async Task InitializeAsync_RunTwice_DoesNotDuplicateApplications()
    {
        await DatabaseInitializer.InitializeAsync(_database, includeDemoApplications: true);

        await using JobTrackerDbContext context = _database.CreateDbContext();
        Assert.That(await context.Applications.CountAsync(), Is.EqualTo(SeedData.CreateApplications().Count));
    }

    [Test]
    public async Task ApplicationService_GivesTheAnalyticsTheSameRecordsAsTheSeedData()
    {
        IReadOnlyList<ApplicationRecord> expected = await new SeedApplicationDataSource().GetApplicationsAsync();
        IReadOnlyList<ApplicationRecord> actual = await new ApplicationService(_database).GetApplicationsAsync();

        Assert.That(actual.Select(Describe).OrderBy(text => text), Is.EqualTo(expected.Select(Describe).OrderBy(text => text)));
    }

    [Test]
    public async Task ResetAsync_WithDemoApplications_PutsDeletedApplicationsBack()
    {
        await using (JobTrackerDbContext context = _database.CreateDbContext())
        {
            await context.StageEvents.ExecuteDeleteAsync();
            await context.Applications.ExecuteDeleteAsync();
        }

        await new DemoDataService(_database, includeDemoApplications: true).ResetAsync();

        await using JobTrackerDbContext after = _database.CreateDbContext();
        Assert.That(await after.Applications.CountAsync(), Is.EqualTo(SeedData.CreateApplications().Count));
    }

    /// <summary>Every field of a record as one line, so a mismatch names the application and field.</summary>
    private static string Describe(ApplicationRecord record) => string.Join(" | ",
        record.Id, record.CompanyName, record.RoleTitle, record.Location, record.WorkMode, record.Source,
        record.SalaryMin?.ToString("0"), record.SalaryMax?.ToString("0"), record.JobDescription.Length, record.DatePosted?.ToString("yyyy-MM-dd"),
        record.DateApplied.ToString("yyyy-MM-dd"), record.DateFirstResponse?.ToString("yyyy-MM-dd"),
        record.HighestStage, record.Outcome, record.HadReferral, record.TailoredResume, record.WroteCoverLetter);
}
