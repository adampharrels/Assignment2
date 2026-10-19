using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Companies;

/// <summary>One company with the applications made to it and the stats shown on the companies page (G4).</summary>
/// <param name="Company">The company itself.</param>
/// <param name="Applications">Every application to the company, newest first; empty when there are none.</param>
/// <param name="BestStage">
/// The furthest pipeline stage any application reached, even if it later ended (e.g. Interview for an
/// application rejected after its interview). Null when the company has no applications.
/// </param>
/// <param name="LastAppliedOn">The most recent applied date, or null when nothing has been sent yet.</param>
public record CompanySummary(
    Company Company,
    IReadOnlyList<JobApplication> Applications,
    ApplicationStatus? BestStage,
    DateTime? LastAppliedOn)
{
    /// <summary>How many applications were made to the company, including wishlist items.</summary>
    public int ApplicationCount => Applications.Count;

    /// <summary>Applications that are still open: not accepted, rejected, withdrawn or ghosted.</summary>
    public int ActiveCount => Applications.Count(application =>
        application.Status is not (ApplicationStatus.Accepted or ApplicationStatus.Rejected
            or ApplicationStatus.Withdrawn or ApplicationStatus.Ghosted));
}
