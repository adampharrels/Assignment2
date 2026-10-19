using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Entities;

/// <summary>One job the user has applied for, or saved to apply for later.</summary>
/// <remarks>
/// Field rules are checked by Validate (via Validator.ValidateObject) in the application service,
/// so forms and imports get the same checks. Status changes go through
/// IApplicationService.ChangeStatusAsync, which also writes a StageEvent; do not set Status directly
/// on an existing application.
/// </remarks>
public class JobApplication : IValidatableObject
{
    /// <summary>The database-generated identifier.</summary>
    public int Id { get; set; }

    /// <summary>
    /// The company's identifier. The application service sets it from the company name typed in the
    /// form, so it is not validated here (a new application has no company yet when the form checks it).
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>The company, when loaded with the application.</summary>
    public Company? Company { get; set; }

    /// <summary>The job title, e.g. "Graduate Software Engineer". Required.</summary>
    [Required(ErrorMessage = "Role is required.")]
    [StringLength(200)]
    public string RoleTitle { get; set; } = string.Empty;

    /// <summary>The city, or "Remote". Required.</summary>
    [Required(ErrorMessage = "Location is required.")]
    [StringLength(100)]
    public string Location { get; set; } = string.Empty;

    /// <summary>On-site, hybrid or remote.</summary>
    [EnumDataType(typeof(WorkMode))]
    public WorkMode WorkMode { get; set; } = WorkMode.OnSite;

    /// <summary>Where the job was found.</summary>
    [EnumDataType(typeof(ApplicationSource))]
    public ApplicationSource Source { get; set; } = ApplicationSource.Other;

    /// <summary>The current status: always the ToStatus of the latest StageEvent.</summary>
    [EnumDataType(typeof(ApplicationStatus))]
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Wishlist;

    /// <summary>Lower bound of the advertised annual salary in AUD, if stated.</summary>
    [Range(0, 10_000_000, ErrorMessage = "Salary can't be negative.")]
    public decimal? SalaryMin { get; set; }

    /// <summary>Upper bound of the advertised annual salary in AUD, if stated. Must be at least SalaryMin.</summary>
    [Range(0, 10_000_000, ErrorMessage = "Salary can't be negative.")]
    public decimal? SalaryMax { get; set; }

    /// <summary>The full job-description text. Skills are extracted from it.</summary>
    public string JobDescription { get; set; } = string.Empty;

    /// <summary>A link to the job ad, if any. Must be a valid URL.</summary>
    [Url(ErrorMessage = "Enter a valid URL, starting with http:// or https://.")]
    public string? JobUrl { get; set; }

    /// <summary>When the job was advertised, if known.</summary>
    public DateTime? DatePosted { get; set; }

    /// <summary>When the application was submitted; null while it is on the wishlist. Can't be in the future.</summary>
    public DateTime? DateApplied { get; set; }

    /// <summary>True when someone inside the company referred the user.</summary>
    public bool HadReferral { get; set; }

    /// <summary>True when the resume was tailored to this job.</summary>
    public bool TailoredResume { get; set; }

    /// <summary>True when a cover letter was sent.</summary>
    public bool WroteCoverLetter { get; set; }

    /// <summary>Personal notes. Included in the free-text search.</summary>
    public string? Notes { get; set; }

    /// <summary>Every status change, oldest first, when loaded with the application.</summary>
    public List<StageEvent> StageHistory { get; set; } = new List<StageEvent>();

    /// <summary>
    /// Rules that involve more than one field: salary range, applied date and posted date.
    /// Single-field rules are the attributes above.
    /// </summary>
    /// <param name="validationContext">Supplied by the validator.</param>
    /// <returns>One result per broken rule, naming the field it belongs to.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SalaryMin.HasValue && SalaryMax.HasValue && SalaryMin.Value > SalaryMax.Value)
        {
            yield return new ValidationResult(
                "Minimum salary can't be more than the maximum.", new[] { nameof(SalaryMin), nameof(SalaryMax) });
        }

        if (DateApplied.HasValue && DateApplied.Value.Date > DateTime.Today)
        {
            yield return new ValidationResult("Applied date can't be in the future.", new[] { nameof(DateApplied) });
        }

        if (DatePosted.HasValue && DateApplied.HasValue && DatePosted.Value.Date > DateApplied.Value.Date)
        {
            yield return new ValidationResult(
                "The job can't be posted after you applied.", new[] { nameof(DatePosted), nameof(DateApplied) });
        }

        if (Status != ApplicationStatus.Wishlist && !DateApplied.HasValue)
        {
            yield return new ValidationResult(
                "Applied date is required once the application is submitted.", new[] { nameof(DateApplied) });
        }
    }
}
