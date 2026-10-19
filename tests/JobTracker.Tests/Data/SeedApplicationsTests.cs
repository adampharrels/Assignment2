using JobTracker.Core.Analytics;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Data;
using JobTracker.Data.Seed;
using JobTracker.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Tests.Data;

/// <summary>K2 (Sarah): the G1 seed applications are saved to the database on first run.</summary>
[TestFixture]
public class SeedApplicationsTests
{
    private TestDatabase _database = null!;
    private ApplicationService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(_database);
        _service = new ApplicationService(_database);
    }

    [TearDown]
    public void TearDown()
    {
        _database.Dispose();
    }

    [Test]
    public async Task Initialize_SavesEverySeedApplicationWithItsIdStatusAndHistory()
    {
        IReadOnlyList<SeedApplication> seeds = SeedData.CreateApplications();
        IReadOnlyList<JobApplication> saved = await _service.GetAllAsync();

        Assert.That(saved.Select(application => application.Id), Is.EquivalentTo(seeds.Select(seed => seed.Id)));

        foreach (SeedApplication seed in seeds)
        {
            JobApplication application = (await _service.GetByIdAsync(seed.Id))!;
            Assert.Multiple(() =>
            {
                Assert.That(application.Company!.Name, Is.EqualTo(seed.CompanyName), $"Company of {seed.Id}");
                Assert.That((int)application.Status, Is.EqualTo((int)seed.Status), $"Status of {seed.Id}");
                Assert.That(application.StageHistory.Select(stageEvent => (int)stageEvent.ToStatus),
                    Is.EqualTo(seed.History.Select(stageEvent => (int)stageEvent.ToStatus)), $"History of {seed.Id}");
            });
        }
    }

    [Test]
    public async Task Initialize_DoesNotAddDuplicateCompanies()
    {
        await using JobTrackerDbContext context = _database.CreateDbContext();

        Assert.That(await context.Companies.CountAsync(), Is.EqualTo(SeedData.CreateCompanies().Count));
    }

    [Test]
    public async Task InitializeTwice_DoesNotSaveTheApplicationsAgain()
    {
        await DatabaseInitializer.InitializeAsync(_database);

        Assert.That(await _service.GetAllAsync(), Has.Count.EqualTo(SeedData.CreateApplications().Count));
    }

    [Test]
    public async Task AnalyticsRecordsFromTheDatabase_MatchTheSeedRecords()
    {
        IReadOnlyList<ApplicationRecord> fromDatabase = await _service.GetApplicationsAsync();
        IReadOnlyList<ApplicationRecord> fromSeed = await new SeedApplicationDataSource().GetApplicationsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(fromDatabase.Select(record => record.Id), Is.EquivalentTo(fromSeed.Select(record => record.Id)));
            Assert.That(fromDatabase.OrderBy(record => record.Id).Select(record => (record.Source, record.WorkMode, record.HighestStage, record.Outcome)),
                Is.EqualTo(fromSeed.OrderBy(record => record.Id).Select(record => (record.Source, record.WorkMode, record.HighestStage, record.Outcome))));
        });
    }

    [Test]
    public async Task NewApplicationAfterSeeding_GetsTheNextFreeId()
    {
        JobApplication created = await _service.CreateAsync(new JobApplication
        {
            RoleTitle = "Graduate Developer",
            Location = "Sydney",
            Status = ApplicationStatus.Applied,
            DateApplied = new DateTime(2026, 10, 1)
        }, "Canva");

        Assert.That(created.Id, Is.GreaterThan(SeedData.CreateApplications().Max(seed => seed.Id)));
    }
}
