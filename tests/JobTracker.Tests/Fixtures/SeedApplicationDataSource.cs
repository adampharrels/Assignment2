using JobTracker.Core.Analytics;
using JobTracker.Core.Interfaces;
using JobTracker.Data;

namespace JobTracker.Tests.Fixtures;

/// <summary>Supplies submitted seed records for tests without accessing the database.</summary>
public class SeedApplicationDataSource : IApplicationDataSource
{
    private static readonly IReadOnlyList<ApplicationRecord> Records = SeedData.CreateApplications()
        .Where(application => application.DateApplied.HasValue)
        .Select(application => application.ToRecord())
        .ToList();

    /// <inheritdoc />
    public Task<IReadOnlyList<ApplicationRecord>> GetApplicationsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Records);
    }
}
