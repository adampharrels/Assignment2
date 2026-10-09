using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Interfaces;

/// <summary>Interview operations shared by the calendar, dashboard and application details.</summary>
/// <remarks>
/// Implementations validate entities, existing application IDs and non-default UTC start times.
/// Invalid input throws ValidationException; missing mutation targets throw KeyNotFoundException.
/// Overlapping non-cancelled interviews across all applications throw InvalidOperationException.
/// Intervals exclude their end, so back-to-back interviews are allowed; updates exclude themselves.
/// Saving an interview does not itself change the application's pipeline status.
/// </remarks>
public interface IInterviewService
{
    /// <summary>Returns an interview, or null if it does not exist.</summary>
    Task<Interview?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns an application's interviews ordered by start time, or an empty list.</summary>
    Task<IReadOnlyList<Interview>> GetByApplicationAsync(int applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns interviews starting at or after startUtc and before endUtc, ordered by start time.
    /// Includes cancelled interviews. Both bounds must be UTC and endUtc must be after startUtc.
    /// </summary>
    Task<IReadOnlyList<Interview>> GetByDateRangeAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    /// <summary>Creates a validated interview and returns it with its generated identifier.</summary>
    Task<Interview> CreateAsync(Interview interview, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing interview, validating its fields and checking for overlaps.</summary>
    Task UpdateAsync(Interview interview, CancellationToken cancellationToken = default);

    /// <summary>Deletes an existing interview.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Records an outcome; restoring a cancelled interview also checks for overlaps.</summary>
    Task SetOutcomeAsync(int id, InterviewOutcome outcome, CancellationToken cancellationToken = default);
}
