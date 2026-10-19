using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Data;
using JobTracker.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Tests.Data;

[TestFixture]
public class ApplicationSchemaTests
{
    private TestDatabase _database = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(_database);
    }

    [TearDown]
    public void TearDown()
    {
        _database.Dispose();
    }

    [Test]
    public async Task Application_WithHistory_SavesAndLoadsWithEnumsAndCompany()
    {
        int applicationId = await SaveApplicationWithHistoryAsync();

        await using JobTrackerDbContext context = _database.CreateDbContext();
        JobApplication loaded = await context.Applications
            .Include(application => application.Company)
            .Include(application => application.StageHistory)
            .SingleAsync(application => application.Id == applicationId);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Company!.Name, Is.EqualTo("K2 Test Co"));
            Assert.That(loaded.Status, Is.EqualTo(ApplicationStatus.Interview));
            Assert.That(loaded.Source, Is.EqualTo(ApplicationSource.Referral));
            Assert.That(loaded.StageHistory.Select(stageEvent => stageEvent.ToStatus),
                Is.EquivalentTo(new[] { ApplicationStatus.Applied, ApplicationStatus.Interview }));
        });
    }

    [Test]
    public async Task DeletingApplication_AlsoDeletesItsStageHistory()
    {
        int applicationId = await SaveApplicationWithHistoryAsync();

        await using (JobTrackerDbContext context = _database.CreateDbContext())
        {
            JobApplication application = await context.Applications.SingleAsync(item => item.Id == applicationId);
            context.Applications.Remove(application);
            await context.SaveChangesAsync();
        }

        await using JobTrackerDbContext check = _database.CreateDbContext();
        Assert.That(await check.StageEvents.AnyAsync(stageEvent => stageEvent.ApplicationId == applicationId), Is.False);
    }

    /// <summary>Saves a company and an application that moved Applied → Interview, and returns its Id.</summary>
    private async Task<int> SaveApplicationWithHistoryAsync()
    {
        await using JobTrackerDbContext context = _database.CreateDbContext();
        var company = new Company { Name = "K2 Test Co" };
        var application = new JobApplication
        {
            Company = company,
            RoleTitle = "Graduate Developer",
            Location = "Sydney",
            Source = ApplicationSource.Referral,
            Status = ApplicationStatus.Interview,
            DateApplied = new DateTime(2026, 9, 1),
            StageHistory = new List<StageEvent>
            {
                new StageEvent { FromStatus = null, ToStatus = ApplicationStatus.Applied, ChangedAt = new DateTime(2026, 9, 1) },
                new StageEvent { FromStatus = ApplicationStatus.Applied, ToStatus = ApplicationStatus.Interview, ChangedAt = new DateTime(2026, 9, 10) }
            }
        };

        context.Applications.Add(application);
        await context.SaveChangesAsync();
        return application.Id;
    }
}
