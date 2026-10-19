using Microsoft.EntityFrameworkCore;

namespace JobTracker.Data.Services;

/// <summary>Puts the database back to the demo data the app starts with (G6, Grace).</summary>
/// <remarks>
/// Used by the "Reset demo data" button on the profile page, e.g. before a demo or after testing an import.
/// Rows are deleted rather than the database file, so it also works while the app has the file open
/// (Windows locks it) and in the in-memory test database.
/// </remarks>
public class DemoDataService
{
    private readonly IDbContextFactory<JobTrackerDbContext> _contextFactory;

    /// <summary>Creates the service.</summary>
    /// <param name="contextFactory">Creates a short-lived database context per call.</param>
    public DemoDataService(IDbContextFactory<JobTrackerDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Deletes everything the user has added or changed (applications, stage history, interviews, companies,
    /// skills and the profile), then adds the starting profile, skills and G1 demo data again.
    /// </summary>
    /// <param name="cancellationToken">Cancels the reset before the delete is committed.</param>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await using (JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken))
        {
            // One transaction, so a failure part-way leaves the old data in place.
            // Children first: interviews and stage events point at applications, which point at companies.
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await context.Interviews.ExecuteDeleteAsync(cancellationToken);
            await context.StageEvents.ExecuteDeleteAsync(cancellationToken);
            await context.Applications.ExecuteDeleteAsync(cancellationToken);
            await context.Companies.ExecuteDeleteAsync(cancellationToken);
            await context.Skills.ExecuteDeleteAsync(cancellationToken);
            await context.UserProfiles.ExecuteDeleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        // Every table is empty now, so the initializer adds the same data as on first run.
        await DatabaseInitializer.InitializeAsync(_contextFactory);
    }
}
