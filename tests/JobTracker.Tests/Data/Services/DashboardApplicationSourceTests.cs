using JobTracker.Core.Dashboard;
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
    public async Task GetAsync_CountsEveryApplicationIncludingWishlist_AndIsNotDemo()
    {
        DashboardApplications result = await new DashboardApplicationSource(_database).GetAsync();

        await using JobTrackerDbContext context = _database.CreateDbContext();
        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(SeedData.CreateApplications().Count));
            Assert.That(result.IsDemo, Is.False);
            Assert.That(result.AddApplicationUrl, Is.EqualTo(DashboardApplicationSource.AddApplicationUrl));
        });
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
}
