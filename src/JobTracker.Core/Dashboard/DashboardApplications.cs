namespace JobTracker.Core.Dashboard;

/// <summary>Application totals and history supplied by the application slice.</summary>
public record DashboardApplications(int Total, IReadOnlyList<StageChange> Changes, bool IsDemo = false,
    string? AddApplicationUrl = null);

/// <summary>A recorded transition, including enough context to display it on the dashboard.</summary>
public record StageChange(int ApplicationId, string Company, string Role, string Stage, DateTime ChangedAt);
