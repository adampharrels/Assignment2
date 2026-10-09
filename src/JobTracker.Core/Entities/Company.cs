using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Entities;

/// <summary>A company to which job applications can be linked.</summary>
public class Company
{
    /// <summary>The database-generated identifier.</summary>
    public int Id { get; set; }

    /// <summary>The required company name; uniqueness is enforced by the company service and database.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>The company's industry, if known.</summary>
    public string? Industry { get; set; }

    /// <summary>The employee-count band, or Unknown when not supplied.</summary>
    [EnumDataType(typeof(CompanySize))]
    public CompanySize Size { get; set; } = CompanySize.Unknown;

    /// <summary>An optional website URL.</summary>
    [Url]
    public string? Website { get; set; }

    /// <summary>Personal notes about the company.</summary>
    public string? Notes { get; set; }
}
