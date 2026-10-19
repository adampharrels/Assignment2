using JobTracker.Core.Dashboard;
using JobTracker.Core.Interfaces;

namespace JobTracker.Data.Services;

/// <summary>Dashboard data built from analytics records only, without stage history. Used by tests; the app uses DashboardApplicationSource.</summary>
/// <remarks>Only submission dates are known; later transitions must never be invented from highest stage.</remarks>
public sealed class DemoDashboardApplicationSource(IApplicationDataSource applications) : IDashboardApplicationSource
{
    /// <inheritdoc />
    public async Task<DashboardApplications> GetAsync(CancellationToken cancellationToken = default)
    {
        var records = await applications.GetApplicationsAsync(cancellationToken);
        return new DashboardApplications(records.Count, records.Select(application => new StageChange(
            application.Id, application.CompanyName, application.RoleTitle, "Submitted",
            application.DateApplied)).ToArray(), IsDemo: true);
    }
}
