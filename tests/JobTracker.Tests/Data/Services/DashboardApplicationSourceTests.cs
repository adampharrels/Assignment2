using JobTracker.Core.Dashboard;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Data;
using JobTracker.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Tests.Data.Services;

[TestFixture]
public class DashboardApplicationSourceTests
{
    private TestDatabase _database = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(_database, includeDemoApplications: true);
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    [Test]
    public async Task GetAsync_CountsEveryApplicationIncludingWishlist()
    {
        DashboardApplications result = await new DashboardApplicationSource(_database).GetAsync();

        Assert.That(result.Total, Is.EqualTo(SeedData.CreateApplications().Count));
    }

    [Test]
    public async Task GetAsync_ReturnsOneChangePerRecordedStageEvent_WithReadableText()
    {
        DashboardApplications result = await new DashboardApplicationSource(_database).GetAsync();

        await using JobTrackerDbContext context = _database.CreateDbContext();
        Assert.Multiple(async () =>
        {
            Assert.That(result.Changes, Has.Count.EqualTo(await context.StageEvents.CountAsync()));
            Assert.That(result.Changes.Select(change => change.Stage), Has.Some.EqualTo("Applied"));
            Assert.That(result.Changes.Select(change => change.Stage), Has.Some.EqualTo("Moved to Final round"));
            Assert.That(result.Changes, Has.All.Matches<StageChange>(change => change.Company.Length > 0 && change.Role.Length > 0));
        });
    }

    [Test]
    public async Task GetAsync_AfterStatusChange_IncludesTheNewChange()
    {
        int appliedId;
        await using (JobTrackerDbContext context = _database.CreateDbContext())
        {
            appliedId = await context.Applications
                .Where(application => application.Status == ApplicationStatus.Applied)
                .Select(application => application.Id)
                .FirstAsync();
        }

        StageEvent changed = await new ApplicationService(_database).ChangeStatusAsync(appliedId, ApplicationStatus.Interview);
        DashboardApplications result = await new DashboardApplicationSource(_database).GetAsync();

        Assert.That(result.Changes, Has.Some.Matches<StageChange>(change =>
            change.ApplicationId == appliedId
            && change.Stage == "Moved to Interview"
            && change.ChangedAt == changed.ChangedAt));
    }
}
