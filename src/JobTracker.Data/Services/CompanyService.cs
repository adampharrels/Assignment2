using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Companies;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Extensions;
using JobTracker.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Data.Services;

/// <summary>Stores companies in the database and works out per-company stats (K3/G4, Grace).</summary>
/// <remarks>
/// A new database context is created for every call, as recommended for Blazor Server.
/// Names are compared after trimming and ignoring case, so "Canva" and " canva " are the same company.
/// </remarks>
public class CompanyService : ICompanyService
{
    private readonly IDbContextFactory<JobTrackerDbContext> _contextFactory;

    /// <summary>Creates the service.</summary>
    /// <param name="contextFactory">Creates a short-lived database context per call.</param>
    public CompanyService(IDbContextFactory<JobTrackerDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        List<Company> companies = await context.Companies.AsNoTracking().ToListAsync(cancellationToken);
        return ByName(companies);
    }

    /// <inheritdoc />
    public async Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Companies.AsNoTracking().FirstOrDefaultAsync(company => company.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Company> CreateAsync(Company company, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(company);

        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        company.Id = 0;
        TrimText(company);
        ValidateCompany(company);
        await EnsureNameIsFreeAsync(context, company.Name, company.Id, cancellationToken);

        context.Companies.Add(company);
        await context.SaveChangesAsync(cancellationToken);
        return company;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(company);

        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        Company existing = await context.Companies.FirstOrDefaultAsync(item => item.Id == company.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Company {company.Id} does not exist.");

        existing.Name = company.Name;
        existing.Industry = company.Industry;
        existing.Size = company.Size;
        existing.Website = company.Website;
        existing.Notes = company.Notes;
        TrimText(existing);
        ValidateCompany(existing);
        await EnsureNameIsFreeAsync(context, existing.Name, existing.Id, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        Company company = await context.Companies.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Company {id} does not exist.");

        int applicationCount = await context.Applications.CountAsync(application => application.CompanyId == id, cancellationToken);
        if (applicationCount > 0)
        {
            string applications = applicationCount == 1 ? "1 application" : $"{applicationCount} applications";
            throw new InvalidOperationException(
                $"{company.Name} still has {applications}. Delete or move them before deleting the company.");
        }

        context.Companies.Remove(company);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Company> GetOrCreateAsync(string name, CancellationToken cancellationToken = default)
    {
        string trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new ValidationException("Company name is required.");
        }

        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        Company? existing = await FindByNameAsync(context, trimmed, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var company = new Company { Name = trimmed };
        ValidateCompany(company);
        context.Companies.Add(company);
        await context.SaveChangesAsync(cancellationToken);
        return company;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompanySummary>> GetSummariesAsync(CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        List<Company> companies = await context.Companies.AsNoTracking().ToListAsync(cancellationToken);
        List<JobApplication> applications = await context.Applications
            .AsNoTracking()
            .Include(application => application.StageHistory)
            .ToListAsync(cancellationToken);

        ILookup<int, JobApplication> byCompany = applications.ToLookup(application => application.CompanyId);

        return ByName(companies)
            .Select(company => Summarise(company, byCompany[company.Id]))
            .ToList();
    }

    /// <summary>Builds the stats for one company from its applications.</summary>
    private static CompanySummary Summarise(Company company, IEnumerable<JobApplication> companyApplications)
    {
        List<JobApplication> applications = companyApplications
            .OrderByDescending(application => application.DateApplied ?? DateTime.MaxValue)
            .ThenByDescending(application => application.Id)
            .ToList();

        ApplicationStatus? bestStage = applications.Count == 0
            ? null
            : applications.Max(BestStageOf);

        DateTime? lastApplied = applications
            .Where(application => application.DateApplied.HasValue)
            .Select(application => application.DateApplied)
            .Max();

        return new CompanySummary(company, applications, bestStage, lastApplied);
    }

    /// <summary>
    /// The furthest pipeline stage one application reached, from its history and current status.
    /// A sent application counts as at least Applied, even if its history is missing.
    /// </summary>
    private static ApplicationStatus BestStageOf(JobApplication application)
    {
        ApplicationStatus furthest = application.StageHistory
            .Append(new StageEvent { ToStatus = application.Status })
            .FurthestPipelineStatus();

        return furthest == ApplicationStatus.Wishlist && application.IsSubmitted()
            ? ApplicationStatus.Applied
            : furthest;
    }

    /// <summary>Finds a company by trimmed, case-insensitive name, or returns null.</summary>
    private static Task<Company?> FindByNameAsync(JobTrackerDbContext context, string name, CancellationToken cancellationToken)
    {
        string lowerName = name.Trim().ToLower();
        return context.Companies.FirstOrDefaultAsync(company => company.Name.ToLower() == lowerName, cancellationToken);
    }

    /// <summary>Throws when another company already has this name (ignoring case and surrounding spaces).</summary>
    /// <exception cref="ValidationException">The name is taken.</exception>
    private static async Task EnsureNameIsFreeAsync(JobTrackerDbContext context, string name, int ownId, CancellationToken cancellationToken)
    {
        Company? match = await FindByNameAsync(context, name, cancellationToken);
        if (match != null && match.Id != ownId)
        {
            throw new ValidationException(
                new ValidationResult($"A company called \"{match.Name}\" already exists.", new[] { nameof(Company.Name) }),
                null,
                name);
        }
    }

    /// <summary>Runs every validation rule and throws one exception that lists all broken rules.</summary>
    /// <exception cref="ValidationException">At least one rule is broken.</exception>
    private static void ValidateCompany(Company company)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(company, new ValidationContext(company), results, validateAllProperties: true))
        {
            throw new ValidationException(string.Join(" ", results.Select(result => result.ErrorMessage)));
        }
    }

    /// <summary>Removes surrounding spaces from the text fields and turns blank optional fields into null.</summary>
    private static void TrimText(Company company)
    {
        company.Name = company.Name?.Trim() ?? string.Empty;
        company.Industry = string.IsNullOrWhiteSpace(company.Industry) ? null : company.Industry.Trim();
        company.Website = string.IsNullOrWhiteSpace(company.Website) ? null : company.Website.Trim();
        company.Notes = string.IsNullOrWhiteSpace(company.Notes) ? null : company.Notes.Trim();
    }

    /// <summary>Companies in alphabetical order, ignoring case.</summary>
    private static List<Company> ByName(IEnumerable<Company> companies) =>
        companies.OrderBy(company => company.Name, StringComparer.OrdinalIgnoreCase).ToList();
}
