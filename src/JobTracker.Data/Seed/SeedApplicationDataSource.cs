using JobTracker.Core.Analytics;
using JobTracker.Core.Interfaces;

namespace JobTracker.Data.Seed;

/// <summary>
/// Gives the analytics the G1 seed applications while the application entity (K2) does not exist.
/// Wishlist items are left out because they were never submitted.
/// </summary>
/// <remarks>Remove the registration in Program.cs once the application service implements IApplicationDataSource.</remarks>
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
