using JobTracker.Core.Enums;

namespace JobTracker.Core.Interfaces;

/// <summary>
/// Search and filter options for the applications list. Every filter that is set must match
/// (they combine with AND); an empty filter returns everything.
/// </summary>
public class ApplicationFilter
{
    /// <summary>Free text matched, ignoring case, against role, company name and notes.</summary>
    public string? SearchText { get; set; }

    /// <summary>Only these statuses; empty means any status.</summary>
    public IReadOnlyCollection<ApplicationStatus> Statuses { get; set; } = Array.Empty<ApplicationStatus>();

    /// <summary>Only these sources; empty means any source.</summary>
    public IReadOnlyCollection<ApplicationSource> Sources { get; set; } = Array.Empty<ApplicationSource>();

    /// <summary>Only these work modes; empty means any work mode.</summary>
    public IReadOnlyCollection<WorkMode> WorkModes { get; set; } = Array.Empty<WorkMode>();

    /// <summary>Location contains this text, ignoring case.</summary>
    public string? Location { get; set; }

    /// <summary>The job description mentions this skill (as recognised by skill extraction).</summary>
    public string? Skill { get; set; }

    /// <summary>The advertised maximum (or minimum, if no maximum) salary is at least this much.</summary>
    public decimal? MinimumSalary { get; set; }

    /// <summary>True when no filter is set.</summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(SearchText)
        && Statuses.Count == 0
        && Sources.Count == 0
        && WorkModes.Count == 0
        && string.IsNullOrWhiteSpace(Location)
        && string.IsNullOrWhiteSpace(Skill)
        && !MinimumSalary.HasValue;
}
