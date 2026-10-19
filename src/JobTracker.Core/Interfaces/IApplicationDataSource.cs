using JobTracker.Core.Analytics;

namespace JobTracker.Core.Interfaces;

/// <summary>Supplies submitted applications to the analytics engine as flat records.</summary>
/// <remarks>
/// The app uses ApplicationService, which reads the stored applications.
/// Wishlist items that were never submitted must not be returned.
/// </remarks>
public interface IApplicationDataSource
{
    /// <summary>Returns every submitted application, or an empty list.</summary>
    Task<IReadOnlyList<ApplicationRecord>> GetApplicationsAsync(CancellationToken cancellationToken = default);
}
