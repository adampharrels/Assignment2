using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Data.Services;

/// <summary>Persists interviews and prevents double-booking across all applications.</summary>
public sealed class InterviewService(IDbContextFactory<JobTrackerDbContext> factory,
    IApplicationDataSource applications) : IInterviewService
{
    // Serializes validation and saving across Blazor circuits in this single-process app.
    private static readonly SemaphoreSlim WriteLock = new(1, 1);

    /// <inheritdoc />
    public async Task<Interview?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var interview = await db.Interviews.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return interview is null ? null : AsUtc(interview);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Interview>> GetByApplicationAsync(int applicationId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var items = await db.Interviews.AsNoTracking().Where(item => item.ApplicationId == applicationId)
            .OrderBy(item => item.ScheduledAt).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        return items.Select(AsUtc).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Interview>> GetByDateRangeAsync(DateTime startUtc, DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc || endUtc <= startUtc)
            throw new ValidationException("Provide a valid UTC date range with the end after the start.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var items = await db.Interviews.AsNoTracking()
            .Where(item => item.ScheduledAt >= startUtc && item.ScheduledAt < endUtc)
            .OrderBy(item => item.ScheduledAt).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        return items.Select(AsUtc).ToArray();
    }

    /// <inheritdoc />
    public async Task<Interview> CreateAsync(Interview interview, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(interview);
        if (interview.Id != 0) throw new ValidationException("A new interview must not already have an ID.");
        return await SaveAsync(interview, true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Interview interview, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(interview);
        await SaveAsync(interview, false, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            var item = await FindAsync(db, id, cancellationToken);
            db.Interviews.Remove(item);
            await db.SaveChangesAsync(cancellationToken);
        }
        finally { WriteLock.Release(); }
    }

    /// <inheritdoc />
    public async Task SetOutcomeAsync(int id, InterviewOutcome outcome, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(outcome)) throw new ValidationException("Choose a valid interview outcome.");
        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            var item = AsUtc(await FindAsync(db, id, cancellationToken));
            item.Outcome = outcome;
            await ValidateAsync(db, item, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        finally { WriteLock.Release(); }
    }

    private async Task<Interview> SaveAsync(Interview input, bool create, CancellationToken token)
    {
        await WriteLock.WaitAsync(token);
        try
        {
            await using var db = await factory.CreateDbContextAsync(token);
            var item = create ? new Interview() : await FindAsync(db, input.Id, token);
            db.Entry(item).CurrentValues.SetValues(input);
            // EF compares DateTime ticks without Kind; preserve the caller's UTC metadata even when unchanged.
            item.ScheduledAt = input.ScheduledAt;
            await ValidateAsync(db, item, token);
            if (create) db.Interviews.Add(item);
            await db.SaveChangesAsync(token);
            return AsUtc(item);
        }
        finally { WriteLock.Release(); }
    }

    private async Task ValidateAsync(JobTrackerDbContext db, Interview item, CancellationToken token)
    {
        Validator.ValidateObject(item, new ValidationContext(item), validateAllProperties: true);
        if (item.ScheduledAt == default || item.ScheduledAt.Kind != DateTimeKind.Utc)
            throw new ValidationException("Choose a valid interview start time in UTC.");
        if (item.ScheduledAt > DateTime.MaxValue.AddMinutes(-item.DurationMinutes))
            throw new ValidationException("The interview end time is outside the supported date range.");
        if (!(await applications.GetApplicationsAsync(token)).Any(application => application.Id == item.ApplicationId))
            throw new ValidationException("The selected application no longer exists. Choose another application.");
        if (item.Outcome == InterviewOutcome.Cancelled) return;
        var end = item.ScheduledAt.AddMinutes(item.DurationMinutes);
        var candidates = await db.Interviews.AsNoTracking()
            .Where(other => other.Id != item.Id && other.Outcome != InterviewOutcome.Cancelled && other.ScheduledAt < end)
            .ToListAsync(token);
        if (candidates.Any(other => other.ScheduledAt.AddMinutes(other.DurationMinutes) > item.ScheduledAt))
            throw new InvalidOperationException("This interview overlaps another interview. Choose a different time or cancel the other interview.");
    }

    private static async Task<Interview> FindAsync(JobTrackerDbContext db, int id, CancellationToken token) =>
        await db.Interviews.SingleOrDefaultAsync(item => item.Id == id, token)
        ?? throw new KeyNotFoundException("The interview no longer exists. Refresh the calendar.");

    private static Interview AsUtc(Interview item)
    {
        item.ScheduledAt = DateTime.SpecifyKind(item.ScheduledAt, DateTimeKind.Utc);
        return item;
    }
}
