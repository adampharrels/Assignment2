using JobTracker.Core.Dashboard;
using JobTracker.Core.Enums;
using JobTracker.Core.Extensions;
using JobTracker.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Data.Services;

/// <summary>
/// Gives the dashboard every stored application, including wishlist items, and the stage changes recorded
/// for them (G2, connected to the database in A7).
/// </summary>
public sealed class DashboardApplicationSource(IDbContextFactory<JobTrackerDbContext> factory) : IDashboardApplicationSource
{
    /// <summary>Opens the applications page with the add form already showing.</summary>
    public const string AddApplicationUrl = "/applications?add=true";

    /// <inheritdoc />
    public async Task<DashboardApplications> GetAsync(CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await factory.CreateDbContextAsync(cancellationToken);
        int total = await context.Applications.CountAsync(cancellationToken);
        var events = await context.StageEvents
            .AsNoTracking()
            .Select(stageEvent => new
            {
                stageEvent.ApplicationId,
                Company = stageEvent.Application!.Company!.Name,
                stageEvent.Application.RoleTitle,
                stageEvent.FromStatus,
                stageEvent.ToStatus,
                stageEvent.ChangedAt
            })
            .ToListAsync(cancellationToken);

        List<StageChange> changes = events
            .Select(item => new StageChange(item.ApplicationId, item.Company, item.RoleTitle,
                Describe(item.FromStatus, item.ToStatus), item.ChangedAt))
            .ToList();
        return new DashboardApplications(total, changes, IsDemo: false, AddApplicationUrl);
    }

    /// <summary>"Applied", "Saved to wishlist" or "Moved to Interview".</summary>
    private static string Describe(ApplicationStatus? from, ApplicationStatus to) => from switch
    {
        null when to == ApplicationStatus.Wishlist => "Saved to wishlist",
        null when to == ApplicationStatus.Applied => "Applied",
        null => $"Added as {to.ToDisplayText()}",
        _ => $"Moved to {to.ToDisplayText()}"
    };
}
