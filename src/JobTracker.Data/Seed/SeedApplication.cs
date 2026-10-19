using JobTracker.Core.Analytics;
using JobTracker.Core.Enums;

namespace JobTracker.Data.Seed;

/// <summary>
/// One demo job application with its full stage history. Has the spec's JobApplication fields so
/// the K2 application service can copy it into its own entity, and maps itself to an
/// ApplicationRecord for the analytics in the meantime.
/// </summary>
public class SeedApplication
{
    /// <summary>The fixed identifier; seeded interviews refer to it.</summary>
    public int Id { get; init; }

    /// <summary>The company name; always one of the seeded companies.</summary>
    public string CompanyName { get; init; } = string.Empty;

    /// <summary>The job title.</summary>
    public string RoleTitle { get; init; } = string.Empty;

    /// <summary>The city, or "Remote".</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>On-site, Hybrid or Remote.</summary>
    public string WorkMode { get; init; } = string.Empty;

    /// <summary>Where the job was found, e.g. "LinkedIn" or "Referral".</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Lower bound of the advertised annual salary in AUD, if stated.</summary>
    public decimal? SalaryMin { get; init; }

    /// <summary>Upper bound of the advertised annual salary in AUD, if stated.</summary>
    public decimal? SalaryMax { get; init; }

    /// <summary>The job-description text.</summary>
    public string JobDescription { get; init; } = string.Empty;

    /// <summary>When the job was advertised.</summary>
    public DateTime? DatePosted { get; init; }

    /// <summary>When the application was submitted, or null for a wishlist item.</summary>
    public DateTime? DateApplied { get; init; }

    /// <summary>True when someone inside the company referred the user.</summary>
    public bool HadReferral { get; init; }

    /// <summary>True when the resume was tailored to this job.</summary>
    public bool TailoredResume { get; init; }

    /// <summary>True when a cover letter was sent.</summary>
    public bool WroteCoverLetter { get; init; }

    /// <summary>Personal notes, if any.</summary>
    public string? Notes { get; init; }

    /// <summary>Every status change in date order, starting with the one that created the application.</summary>
    public IReadOnlyList<SeedStageEvent> History { get; init; } = Array.Empty<SeedStageEvent>();

    /// <summary>The current status: the last status in the history.</summary>
    public ApplicationStatus Status => History.Count == 0 ? ApplicationStatus.Wishlist : History[^1].ToStatus;

    /// <summary>The furthest pipeline stage ever reached, even if the application was later rejected.</summary>
    public PipelineStage HighestStage
    {
        get
        {
            PipelineStage highest = PipelineStage.Applied;
            foreach (SeedStageEvent stageEvent in History)
            {
                PipelineStage? stage = ToPipelineStage(stageEvent.ToStatus);
                if (stage.HasValue && stage.Value > highest)
                {
                    highest = stage.Value;
                }
            }

            return highest;
        }
    }

    /// <summary>How the application ended, or InProgress while it is still open.</summary>
    public ApplicationOutcome Outcome => Status switch
    {
        ApplicationStatus.Accepted => ApplicationOutcome.Accepted,
        ApplicationStatus.Rejected => ApplicationOutcome.Rejected,
        ApplicationStatus.Withdrawn => ApplicationOutcome.Withdrawn,
        ApplicationStatus.Ghosted => ApplicationOutcome.Ghosted,
        _ => ApplicationOutcome.InProgress
    };

    /// <summary>
    /// The date the company first replied: the first change after submitting that the company caused.
    /// Ghosting and withdrawing are not replies.
    /// </summary>
    public DateTime? DateFirstResponse
    {
        get
        {
            SeedStageEvent? reply = History.FirstOrDefault(stageEvent =>
                stageEvent.FromStatus.HasValue
                && stageEvent.FromStatus != ApplicationStatus.Wishlist
                && stageEvent.ToStatus != ApplicationStatus.Ghosted
                && stageEvent.ToStatus != ApplicationStatus.Withdrawn);
            return reply?.ChangedAt;
        }
    }

    /// <summary>Flattens the application for the analytics engine.</summary>
    /// <exception cref="InvalidOperationException">The application is a wishlist item that was never submitted.</exception>
    public ApplicationRecord ToRecord()
    {
        if (!DateApplied.HasValue)
        {
            throw new InvalidOperationException($"Seed application {Id} is on the wishlist and was never submitted.");
        }

        return new ApplicationRecord
        {
            Id = Id,
            CompanyName = CompanyName,
            RoleTitle = RoleTitle,
            Location = Location,
            WorkMode = WorkMode,
            Source = Source,
            SalaryMin = SalaryMin,
            SalaryMax = SalaryMax,
            JobDescription = JobDescription,
            DatePosted = DatePosted,
            DateApplied = DateApplied.Value,
            DateFirstResponse = DateFirstResponse,
            HighestStage = HighestStage,
            Outcome = Outcome,
            HadReferral = HadReferral,
            TailoredResume = TailoredResume,
            WroteCoverLetter = WroteCoverLetter
        };
    }

    /// <summary>The pipeline stage a status counts as, or null for statuses outside the pipeline.</summary>
    private static PipelineStage? ToPipelineStage(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Applied => PipelineStage.Applied,
        ApplicationStatus.Assessment => PipelineStage.Assessment,
        ApplicationStatus.Interview => PipelineStage.Interview,
        ApplicationStatus.FinalRound => PipelineStage.FinalRound,
        ApplicationStatus.Offer => PipelineStage.Offer,
        ApplicationStatus.Accepted => PipelineStage.Offer,
        _ => null
    };
}
