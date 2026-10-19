using JobTracker.Core.Companies;
using JobTracker.Core.Entities;

namespace JobTracker.Core.Interfaces;

/// <summary>Company persistence operations shared by company and application screens.</summary>
/// <remarks>
/// Implementations validate entities before saving and reject duplicate names after trimming
/// and case-insensitive comparison. Invalid input throws ValidationException; missing mutation
/// targets throw KeyNotFoundException. These are contracts, not persistence implementations.
/// </remarks>
public interface ICompanyService
{
    /// <summary>Returns all companies ordered by name, or an empty list.</summary>
    Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a company, or null if it does not exist.</summary>
    Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates a validated company and returns it with its generated identifier.</summary>
    Task<Company> CreateAsync(Company company, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing company after validating its fields and name uniqueness.</summary>
    Task UpdateAsync(Company company, CancellationToken cancellationToken = default);

    /// <summary>Deletes a company; throws InvalidOperationException if applications still reference it.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Finds a company by trimmed, case-insensitive name, or creates it for an application form.</summary>
    Task<Company> GetOrCreateAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every company ordered by name, with its applications, application count and best stage
    /// reached (G4). Companies without applications are included with an empty list.
    /// </summary>
    Task<IReadOnlyList<CompanySummary>> GetSummariesAsync(CancellationToken cancellationToken = default);
}
