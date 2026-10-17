using JobTracker.Core.Analytics;
using JobTracker.Core.Dashboard;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Data.Services;

/// <summary>A freshly loaded dashboard, with bounded history and insights.</summary>
public record DashboardSnapshot(PipelineSummary Summary, DashboardApplications Applications,
    IReadOnlyList<Interview> Interviews, IReadOnlyList<StageChange> Activity, IReadOnlyList<Insight> Insights);

/// <summary>Combines analytics, application history and persisted interviews for the home page.</summary>
public sealed class DashboardService(IAnalyticsService analytics, IDashboardApplicationSource applications,
    IDbContextFactory<JobTrackerDbContext> factory)
{
    /// <summary>Loads the next seven days (inclusive start, exclusive end), excluding cancelled interviews.</summary>
    public async Task<DashboardSnapshot> LoadAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The dashboard clock must use UTC.", nameof(nowUtc));

        var summary = await analytics.GetSummaryAsync(cancellationToken);
        var applicationData = await applications.GetAsync(cancellationToken);
        var insights = await analytics.GetInsightsAsync(cancellationToken);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var end = nowUtc.AddDays(7);
        var interviews = await db.Interviews.AsNoTracking()
            .Where(interview => interview.ScheduledAt >= nowUtc && interview.ScheduledAt < end
                && interview.Outcome != InterviewOutcome.Cancelled)
            .OrderBy(interview => interview.ScheduledAt).ThenBy(interview => interview.Id)
            .ToListAsync(cancellationToken);
        return new DashboardSnapshot(summary, applicationData, interviews,
            applicationData.Changes.OrderByDescending(change => change.ChangedAt)
                .ThenByDescending(change => change.ApplicationId).Take(5).ToArray(),
            insights.OrderByDescending(insight => insight.Importance).Take(3).ToArray());
    }
}
