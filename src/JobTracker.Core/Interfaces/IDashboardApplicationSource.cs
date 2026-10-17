using JobTracker.Core.Dashboard;

namespace JobTracker.Core.Interfaces;

/// <summary>Supplies all-application totals (including wishlist), history and the creation route.</summary>
public interface IDashboardApplicationSource
{
    /// <summary>Reads current totals and stage changes without caching between dashboard visits.</summary>
    Task<DashboardApplications> GetAsync(CancellationToken cancellationToken = default);
}
