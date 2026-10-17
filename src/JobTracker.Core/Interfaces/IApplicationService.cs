using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Interfaces;

/// <summary>
/// Job application operations shared by the board, the applications list, the details page,
/// the dashboard and CSV/JSON import.
/// </summary>
/// <remarks>
/// <para>
/// Implementations validate every application with Validator.ValidateObject (the attributes and
/// JobApplication.Validate) and throw ValidationException when it is invalid. Missing mutation targets
/// throw KeyNotFoundException. Status moves follow ApplicationStatusRules; an illegal move throws
/// InvalidOperationException whose message can be shown to the user as it is.
/// </para>
/// <para>
/// Every status change, including the first one when an application is created, writes a StageEvent.
/// Moving to Applied sets DateApplied to today when it is empty. After every successful change the
/// service raises ApplicationsChanged so that open screens (for example the dashboard) can refresh.
/// </para>
/// <para>
/// The implementation also implements IApplicationDataSource by mapping submitted applications with
/// ApplicationExtensions.ToRecord, so the analytics read real data.
/// </para>
/// </remarks>
public interface IApplicationService
{
    /// <summary>Raised after an application is created, edited, moved or deleted.</summary>
    event EventHandler<ApplicationChangedEventArgs>? ApplicationsChanged;

    /// <summary>Returns every application with its company, newest first, or an empty list.</summary>
    Task<IReadOnlyList<JobApplication>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the applications that match every filter that is set, with their companies, newest first.</summary>
    /// <param name="filter">The search text and filters; an empty filter returns everything.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<IReadOnlyList<JobApplication>> SearchAsync(ApplicationFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Returns an application with its company and stage history (oldest first), or null if it does not exist.</summary>
    Task<JobApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns an application's status changes, oldest first, or an empty list.</summary>
    Task<IReadOnlyList<StageEvent>> GetHistoryAsync(int applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and saves a new application, writes its first StageEvent (from null to its Status)
    /// and extracts skills from its job description.
    /// </summary>
    /// <param name="application">The new application. Its Status is the starting status.</param>
    /// <param name="companyName">
    /// The company typed or picked in the form. An existing company is matched by name, ignoring case;
    /// otherwise a new company is created (ICompanyService.GetOrCreateAsync).
    /// </param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>The saved application with its generated identifier.</returns>
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">A field is invalid.</exception>
    Task<JobApplication> CreateAsync(JobApplication application, string companyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and saves edits to an application's details. Does not change its status:
    /// use ChangeStatusAsync for that, so the history stays complete.
    /// </summary>
    /// <param name="application">The edited application; matched by Id.</param>
    /// <param name="companyName">The company, matched or created as in CreateAsync.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">A field is invalid.</exception>
    /// <exception cref="KeyNotFoundException">No application has that Id.</exception>
    Task UpdateAsync(JobApplication application, string companyName, CancellationToken cancellationToken = default);

    /// <summary>Moves an application to a new status and records the StageEvent.</summary>
    /// <param name="applicationId">The application to move.</param>
    /// <param name="newStatus">The status to move to.</param>
    /// <param name="note">An optional note saved with the StageEvent.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>The StageEvent that was written.</returns>
    /// <exception cref="InvalidOperationException">The move breaks a status rule; the message explains why.</exception>
    /// <exception cref="KeyNotFoundException">No application has that Id.</exception>
    Task<StageEvent> ChangeStatusAsync(int applicationId, ApplicationStatus newStatus, string? note = null, CancellationToken cancellationToken = default);

    /// <summary>Deletes an application together with its stage history and its interviews.</summary>
    /// <exception cref="KeyNotFoundException">No application has that Id.</exception>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
