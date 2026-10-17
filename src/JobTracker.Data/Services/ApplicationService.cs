using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Analytics;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Extensions;
using JobTracker.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Data.Services;

/// <summary>
/// Stores job applications and their stage history in the database (K2/S1, Sarah).
/// Also supplies submitted applications to the analytics through IApplicationDataSource.
/// </summary>
/// <remarks>
/// A new database context is created for every call, as recommended for Blazor Server.
/// Until Grace's company service (G4) is merged, companies are matched or created here by name;
/// switch FindOrCreateCompanyAsync to ICompanyService.GetOrCreateAsync once it exists.
/// </remarks>
public class ApplicationService : IApplicationService, IApplicationDataSource
{
    private readonly IDbContextFactory<JobTrackerDbContext> _contextFactory;

    /// <summary>Creates the service.</summary>
    /// <param name="contextFactory">Creates a short-lived database context per call.</param>
    public ApplicationService(IDbContextFactory<JobTrackerDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public event EventHandler<ApplicationChangedEventArgs>? ApplicationsChanged;

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobApplication>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        List<JobApplication> applications = await context.Applications
            .AsNoTracking()
            .Include(application => application.Company)
            .ToListAsync(cancellationToken);

        return NewestFirst(applications);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobApplication>> SearchAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // The list holds a few dozen applications, so filtering in memory with LINQ is simple and fast,
        // and avoids SQLite's limits on comparing decimal (salary) values.
        IEnumerable<JobApplication> results = await GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            string text = filter.SearchText.Trim();
            results = results.Where(application =>
                Contains(application.RoleTitle, text)
                || Contains(application.Company?.Name, text)
                || Contains(application.Notes, text));
        }

        if (filter.Statuses.Count > 0)
        {
            results = results.Where(application => filter.Statuses.Contains(application.Status));
        }

        if (filter.Sources.Count > 0)
        {
            results = results.Where(application => filter.Sources.Contains(application.Source));
        }

        if (filter.WorkModes.Count > 0)
        {
            results = results.Where(application => filter.WorkModes.Contains(application.WorkMode));
        }

        if (!string.IsNullOrWhiteSpace(filter.Location))
        {
            results = results.Where(application => Contains(application.Location, filter.Location.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Skill))
        {
            results = results.Where(application => Contains(application.JobDescription, filter.Skill.Trim()));
        }

        if (filter.MinimumSalary.HasValue)
        {
            results = results.Where(application =>
                (application.SalaryMax ?? application.SalaryMin) is decimal salary && salary >= filter.MinimumSalary.Value);
        }

        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<JobApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        JobApplication? application = await context.Applications
            .AsNoTracking()
            .Include(item => item.Company)
            .Include(item => item.StageHistory)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (application != null)
        {
            application.StageHistory = application.StageHistory.OrderBy(stageEvent => stageEvent.ChangedAt).ToList();
        }

        return application;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetCompanyNamesAsync(CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        List<string> names = await context.Companies.AsNoTracking().Select(company => company.Name).ToListAsync(cancellationToken);
        return names.Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StageEvent>> GetHistoryAsync(int applicationId, CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        List<StageEvent> history = await context.StageEvents
            .AsNoTracking()
            .Where(stageEvent => stageEvent.ApplicationId == applicationId)
            .ToListAsync(cancellationToken);

        return history.OrderBy(stageEvent => stageEvent.ChangedAt).ToList();
    }

    /// <inheritdoc />
    public async Task<JobApplication> CreateAsync(JobApplication application, string companyName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(application);

        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        Company company = await FindOrCreateCompanyAsync(context, companyName, cancellationToken);

        application.Id = 0;
        application.Company = company;
        application.CompanyId = company.Id;
        TrimText(application);
        ValidateApplication(application);

        application.StageHistory = new List<StageEvent>
        {
            new StageEvent
            {
                FromStatus = null,
                ToStatus = application.Status,
                ChangedAt = application.DateApplied ?? DateTime.Now,
                Note = "Application added"
            }
        };

        context.Applications.Add(application);
        await context.SaveChangesAsync(cancellationToken);

        OnApplicationsChanged(application.Id, ApplicationChangeKind.Created);
        return application;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(JobApplication application, string companyName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(application);

        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        JobApplication existing = await context.Applications.FirstOrDefaultAsync(item => item.Id == application.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Application {application.Id} does not exist.");

        Company company = await FindOrCreateCompanyAsync(context, companyName, cancellationToken);

        // Copy the editable details only. Status changes go through ChangeStatusAsync so the history stays complete.
        existing.Company = company;
        existing.CompanyId = company.Id;
        existing.RoleTitle = application.RoleTitle;
        existing.Location = application.Location;
        existing.WorkMode = application.WorkMode;
        existing.Source = application.Source;
        existing.SalaryMin = application.SalaryMin;
        existing.SalaryMax = application.SalaryMax;
        existing.JobDescription = application.JobDescription;
        existing.JobUrl = application.JobUrl;
        existing.DatePosted = application.DatePosted;
        existing.DateApplied = application.DateApplied;
        existing.HadReferral = application.HadReferral;
        existing.TailoredResume = application.TailoredResume;
        existing.WroteCoverLetter = application.WroteCoverLetter;
        existing.Notes = application.Notes;
        TrimText(existing);
        ValidateApplication(existing);

        await context.SaveChangesAsync(cancellationToken);
        OnApplicationsChanged(existing.Id, ApplicationChangeKind.Updated);
    }

    /// <inheritdoc />
    public async Task<StageEvent> ChangeStatusAsync(int applicationId, ApplicationStatus newStatus, string? note = null, CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        JobApplication application = await context.Applications
            .Include(item => item.StageHistory)
            .FirstOrDefaultAsync(item => item.Id == applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application {applicationId} does not exist.");

        ApplicationStatus furthest = application.StageHistory
            .Append(new StageEvent { ToStatus = application.Status })
            .FurthestPipelineStatus();
        ApplicationStatusRules.EnsureCanMove(application.Status, newStatus, furthest);

        // Leaving the wishlist means the application was sent today, unless a date was already entered.
        if (application.Status == ApplicationStatus.Wishlist
            && newStatus != ApplicationStatus.Withdrawn
            && !application.DateApplied.HasValue)
        {
            application.DateApplied = DateTime.Today;
        }

        var stageEvent = new StageEvent
        {
            ApplicationId = application.Id,
            FromStatus = application.Status,
            ToStatus = newStatus,
            ChangedAt = DateTime.Now,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };

        application.Status = newStatus;
        application.StageHistory.Add(stageEvent);
        await context.SaveChangesAsync(cancellationToken);

        OnApplicationsChanged(application.Id, ApplicationChangeKind.StatusChanged);
        return stageEvent;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        JobApplication application = await context.Applications.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Application {id} does not exist.");

        // Interviews have no foreign key to applications yet (see JobTrackerDbContext), so remove them here.
        // The stage history is removed by the cascade delete.
        List<Interview> interviews = await context.Interviews.Where(interview => interview.ApplicationId == id).ToListAsync(cancellationToken);
        context.Interviews.RemoveRange(interviews);
        context.Applications.Remove(application);
        await context.SaveChangesAsync(cancellationToken);

        OnApplicationsChanged(id, ApplicationChangeKind.Deleted);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApplicationRecord>> GetApplicationsAsync(CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        List<JobApplication> applications = await context.Applications
            .AsNoTracking()
            .Include(application => application.Company)
            .Include(application => application.StageHistory)
            .ToListAsync(cancellationToken);

        return applications
            .Where(application => application.IsSubmitted())
            .Select(application => application.ToRecord())
            .ToList();
    }

    /// <summary>Raises ApplicationsChanged so that open screens can refresh.</summary>
    protected virtual void OnApplicationsChanged(int applicationId, ApplicationChangeKind kind)
    {
        ApplicationsChanged?.Invoke(this, new ApplicationChangedEventArgs(applicationId, kind));
    }

    /// <summary>Finds a company by name (ignoring case and surrounding spaces), or adds a new one to the context.</summary>
    /// <exception cref="ValidationException">The name is empty.</exception>
    private static async Task<Company> FindOrCreateCompanyAsync(JobTrackerDbContext context, string? companyName, CancellationToken cancellationToken)
    {
        string name = companyName?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            throw new ValidationException("Company is required.");
        }

        string lowerName = name.ToLower();
        Company? company = await context.Companies.FirstOrDefaultAsync(item => item.Name.ToLower() == lowerName, cancellationToken);
        if (company == null)
        {
            company = new Company { Name = name };
            context.Companies.Add(company);
        }

        return company;
    }

    /// <summary>Runs every validation rule and throws one exception that lists all broken rules.</summary>
    /// <exception cref="ValidationException">At least one rule is broken.</exception>
    private static void ValidateApplication(JobApplication application)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(application, new ValidationContext(application), results, validateAllProperties: true))
        {
            throw new ValidationException(string.Join(" ", results.Select(result => result.ErrorMessage)));
        }
    }

    /// <summary>Removes surrounding spaces from the text fields and turns blank optional fields into null.</summary>
    private static void TrimText(JobApplication application)
    {
        application.RoleTitle = application.RoleTitle?.Trim() ?? string.Empty;
        application.Location = application.Location?.Trim() ?? string.Empty;
        application.JobDescription = application.JobDescription?.Trim() ?? string.Empty;
        application.JobUrl = string.IsNullOrWhiteSpace(application.JobUrl) ? null : application.JobUrl.Trim();
        application.Notes = string.IsNullOrWhiteSpace(application.Notes) ? null : application.Notes.Trim();
    }

    /// <summary>Newest applications first: by applied date (wishlist items count as today), then by Id.</summary>
    private static List<JobApplication> NewestFirst(IEnumerable<JobApplication> applications) =>
        applications
            .OrderByDescending(application => application.DateApplied ?? DateTime.Today)
            .ThenByDescending(application => application.Id)
            .ToList();

    /// <summary>Case-insensitive "contains" that treats null as no match.</summary>
    private static bool Contains(string? value, string text) =>
        value != null && value.Contains(text, StringComparison.OrdinalIgnoreCase);
}
