using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Entities;

/// <summary>A company to which job applications can be linked.</summary>
/// <remarks>
/// Field rules are checked here (attributes and Validate), so the company form and the company service
/// use the same checks. Name uniqueness needs the database, so only the company service checks it.
/// </remarks>
public class Company : IValidatableObject
{
    /// <summary>The database-generated identifier.</summary>
    public int Id { get; set; }

    /// <summary>The required company name; uniqueness is enforced by the company service and database.</summary>
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200, ErrorMessage = "Company name can't be longer than 200 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The company's industry, if known.</summary>
    [StringLength(100, ErrorMessage = "Industry can't be longer than 100 characters.")]
    public string? Industry { get; set; }

    /// <summary>The employee-count band, or Unknown when not supplied.</summary>
    [EnumDataType(typeof(CompanySize))]
    public CompanySize Size { get; set; } = CompanySize.Unknown;

    /// <summary>An optional website URL, starting with http:// or https://.</summary>
    [Url(ErrorMessage = WebsiteError)]
    public string? Website { get; set; }

    /// <summary>Personal notes about the company.</summary>
    public string? Notes { get; set; }

    private const string WebsiteError = "Enter a valid website, e.g. https://www.example.com.";

    /// <summary>
    /// True when the text is an absolute http or https address with a dotted host name, e.g.
    /// "https://www.canva.com". The [Url] attribute alone accepts "https://" or "ftp://x".
    /// </summary>
    /// <param name="website">The text to check.</param>
    public static bool IsValidWebsite(string? website) =>
        Uri.TryCreate(website?.Trim(), UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.Host.Contains('.')
        && !uri.Host.StartsWith('.')
        && !uri.Host.EndsWith('.');

    /// <summary>Checks the website more strictly than the [Url] attribute does.</summary>
    /// <param name="validationContext">Supplied by the validator.</param>
    /// <returns>One result per broken rule, naming the field it belongs to.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Website) && !IsValidWebsite(Website))
        {
            yield return new ValidationResult(WebsiteError, new[] { nameof(Website) });
        }
    }
}
