using JobTracker.Core.Analytics;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Extensions;

/// <summary>
/// Derived facts about an application, worked out from its stage history, and the mapping to the
/// flat ApplicationRecord that the analytics read through IApplicationDataSource.
/// </summary>
public static class ApplicationExtensions
{
    /// <summary>True once the application has been sent (it is no longer on the wishlist).</summary>
    public static bool IsSubmitted(this JobApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        return application.DateApplied.HasValue && application.Status != ApplicationStatus.Wishlist;
    }

    /// <summary>
    /// The furthest analytics stage the application reached, even if it was rejected later.
    /// Accepted counts as Offer. Uses the stage history, so load it first.
    /// </summary>
    public static PipelineStage HighestStage(this JobApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        ApplicationStatus furthest = application.StageHistory
            .Append(new StageEvent { ToStatus = application.Status })
            .FurthestPipelineStatus();

        return furthest switch
        {
            ApplicationStatus.Assessment => PipelineStage.Assessment,
            ApplicationStatus.Interview => PipelineStage.Interview,
            ApplicationStatus.FinalRound => PipelineStage.FinalRound,
            ApplicationStatus.Offer or ApplicationStatus.Accepted => PipelineStage.Offer,
            _ => PipelineStage.Applied
        };
    }

    /// <summary>How the application ended, or InProgress while it is still open.</summary>
    public static ApplicationOutcome Outcome(this JobApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        return application.Status switch
        {
            ApplicationStatus.Accepted => ApplicationOutcome.Accepted,
            ApplicationStatus.Rejected => ApplicationOutcome.Rejected,
            ApplicationStatus.Withdrawn => ApplicationOutcome.Withdrawn,
            ApplicationStatus.Ghosted => ApplicationOutcome.Ghosted,
            _ => ApplicationOutcome.InProgress
        };
    }

    /// <summary>
    /// When the company first replied: the first change after submitting that the company caused.
    /// Ghosting and withdrawing are not replies. Null when there has been no reply.
    /// </summary>
    public static DateTime? DateFirstResponse(this JobApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        return application.StageHistory
            .OrderBy(stageEvent => stageEvent.ChangedAt)
            .FirstOrDefault(stageEvent =>
                stageEvent.FromStatus.HasValue
                && stageEvent.FromStatus != ApplicationStatus.Wishlist
                && stageEvent.ToStatus != ApplicationStatus.Ghosted
                && stageEvent.ToStatus != ApplicationStatus.Withdrawn)
            ?.ChangedAt;
    }

    /// <summary>Flattens a submitted application for the analytics engine. Load Company and StageHistory first.</summary>
    /// <exception cref="InvalidOperationException">The application is still on the wishlist.</exception>
    public static ApplicationRecord ToRecord(this JobApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (!application.IsSubmitted())
        {
            throw new InvalidOperationException(
                $"Application {application.Id} is on the wishlist, so it has no analytics record.");
        }

        return new ApplicationRecord
        {
            Id = application.Id,
            CompanyName = application.Company?.Name ?? string.Empty,
            RoleTitle = application.RoleTitle,
            Location = application.Location,
            WorkMode = application.WorkMode.ToDisplayText(),
            Source = application.Source.ToDisplayText(),
            SalaryMin = application.SalaryMin,
            SalaryMax = application.SalaryMax,
            JobDescription = application.JobDescription,
            DatePosted = application.DatePosted,
            DateApplied = application.DateApplied!.Value,
            DateFirstResponse = application.DateFirstResponse(),
            HighestStage = application.HighestStage(),
            Outcome = application.Outcome(),
            HadReferral = application.HadReferral,
            TailoredResume = application.TailoredResume,
            WroteCoverLetter = application.WroteCoverLetter
        };
    }
}
