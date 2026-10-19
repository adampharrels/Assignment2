using JobTracker.Core.Enums;

namespace JobTracker.Core.Entities;

/// <summary>
/// One status change of an application. A StageEvent is written on every change, including the
/// one that creates the application, so the history shows the furthest stage ever reached even
/// after a rejection.
/// </summary>
public class StageEvent
{
    /// <summary>The database-generated identifier.</summary>
    public int Id { get; set; }

    /// <summary>The application's identifier.</summary>
    public int ApplicationId { get; set; }

    /// <summary>The application, when loaded with the event.</summary>
    public JobApplication? Application { get; set; }

    /// <summary>The status before the change, or null for the event that created the application.</summary>
    public ApplicationStatus? FromStatus { get; set; }

    /// <summary>The status after the change.</summary>
    public ApplicationStatus ToStatus { get; set; }

    /// <summary>When the change happened.</summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>An optional note, e.g. "Recruiter called".</summary>
    public string? Note { get; set; }
}
