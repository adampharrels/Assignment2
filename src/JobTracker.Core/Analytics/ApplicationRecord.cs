namespace JobTracker.Core.Analytics;

/// <summary>
/// A flattened, read-only view of one submitted application: the only input the analytics and
/// ML code needs. Keeping analytics on its own input type means it does not depend on how the
/// application entity (K2) is stored; the application service only has to map its entity to this.
/// </summary>
public class ApplicationRecord
{
    /// <summary>The application's identifier.</summary>
    public int Id { get; set; }

    /// <summary>The company name.</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>The job title, e.g. "Graduate Frontend Engineer".</summary>
    public string RoleTitle { get; set; } = string.Empty;

    /// <summary>The city, or "Remote".</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>On-site, Hybrid or Remote, as display text.</summary>
    public string WorkMode { get; set; } = string.Empty;

    /// <summary>Where the job was found, e.g. "LinkedIn" or "Referral", as display text.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Lower bound of the advertised annual salary in AUD, if known.</summary>
    public decimal? SalaryMin { get; set; }

    /// <summary>Upper bound of the advertised annual salary in AUD, if known.</summary>
    public decimal? SalaryMax { get; set; }

    /// <summary>The full job-description text. Input to skill extraction and TF-IDF.</summary>
    public string JobDescription { get; set; } = string.Empty;

    /// <summary>When the job was advertised, if known.</summary>
    public DateTime? DatePosted { get; set; }

    /// <summary>When the application was submitted.</summary>
    public DateTime DateApplied { get; set; }

    /// <summary>When the company first replied (any reply, including a rejection), if it has.</summary>
    public DateTime? DateFirstResponse { get; set; }

    /// <summary>The furthest stage reached, even if the application was later rejected.</summary>
    public PipelineStage HighestStage { get; set; } = PipelineStage.Applied;

    /// <summary>How the application ended, or InProgress.</summary>
    public ApplicationOutcome Outcome { get; set; } = ApplicationOutcome.InProgress;

    /// <summary>True when someone inside the company referred the user.</summary>
    public bool HadReferral { get; set; }

    /// <summary>True when the resume was tailored to this job.</summary>
    public bool TailoredResume { get; set; }

    /// <summary>True when a cover letter was sent.</summary>
    public bool WroteCoverLetter { get; set; }
}
