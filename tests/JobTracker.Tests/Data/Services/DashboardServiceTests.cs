using JobTracker.Analytics;
using JobTracker.Analytics.Text;
using JobTracker.Core.Analytics;
using JobTracker.Core.Dashboard;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Interfaces;
using JobTracker.Data.Services;
using JobTracker.Tests.Data;

namespace JobTracker.Tests.Data.Services;

[TestFixture]
public class DashboardServiceTests
{
    [Test]
    public async Task Load_FiltersSevenDayWindowAndCancelledInterviews_AndReadsChangesAgain()
    {
        using var database = new TestDatabase();
        await using var db = database.CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        var now = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc);
        db.Interviews.AddRange(
            new Interview { ApplicationId = 1, ScheduledAt = now.AddDays(7) },
            new Interview { ApplicationId = 1, ScheduledAt = now.AddDays(1) },
            new Interview { ApplicationId = 1, ScheduledAt = now },
            new Interview { ApplicationId = 1, ScheduledAt = now.AddTicks(-1) },
            new Interview { ApplicationId = 1, ScheduledAt = now.AddHours(1), Outcome = InterviewOutcome.Cancelled });
        await db.SaveChangesAsync();
        var source = new ApplicationSource();
        var service = CreateService(database, source);
        var result = await service.LoadAsync(now);
        Assert.That(result.Interviews.Select(item => item.ScheduledAt), Is.EqualTo(new[] { now, now.AddDays(1) }));
        Assert.That(result.Applications.Total, Is.EqualTo(9));
        Assert.That(result.Summary.TotalApplications, Is.EqualTo(8));
        Assert.That(result.Activity.Select(item => item.ApplicationId), Is.EqualTo(new[] { 8, 7, 6, 5, 4 }));
        Assert.That(result.Insights.Count, Is.LessThanOrEqualTo(3));

        source.Records.Clear();
        db.Interviews.Add(new Interview { ApplicationId = 2, ScheduledAt = now.AddHours(2) });
        await db.SaveChangesAsync();
        result = await service.LoadAsync(now);
        Assert.That(result.Summary.TotalApplications, Is.Zero);
        Assert.That(result.Activity, Is.Empty);
        Assert.That(result.Insights, Is.Empty);
        Assert.That(result.Interviews.Count, Is.EqualTo(3));
    }

    [Test]
    public void Load_RejectsAmbiguousClock()
    {
        using var database = new TestDatabase();
        var service = CreateService(database, new ApplicationSource());
        Assert.ThrowsAsync<ArgumentException>(() => service.LoadAsync(new DateTime(2026, 10, 18)));
    }

    private static DashboardService CreateService(TestDatabase database, ApplicationSource source) =>
        new(new AnalyticsService(source, new ProfileService(database), new DictionarySkillExtractor()), source, database);

    private sealed class ApplicationSource : IApplicationDataSource, IDashboardApplicationSource
    {
        public List<ApplicationRecord> Records { get; } = Enumerable.Range(1, 8).Select(id => new ApplicationRecord
        {
            Id = id,
            CompanyName = "Company",
            RoleTitle = "Graduate",
            DateApplied = new DateTime(2026, 10, id)
        }).ToList();

        public Task<IReadOnlyList<ApplicationRecord>> GetApplicationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ApplicationRecord>>(Records.ToArray());

        public Task<DashboardApplications> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new DashboardApplications(Records.Count + 1, Records.Select(item =>
                new StageChange(item.Id, item.CompanyName, item.RoleTitle, "Submitted", item.DateApplied)).ToArray()));
    }
}
