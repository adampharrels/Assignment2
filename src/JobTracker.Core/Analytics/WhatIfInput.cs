namespace JobTracker.Core.Analytics;

/// <summary>A job the user is thinking of applying for, used by the what-if predictor.</summary>
public class WhatIfInput
{
    /// <summary>The pasted job description.</summary>
    public string JobDescription { get; set; } = string.Empty;

    /// <summary>Where the job was found.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>On-site, Hybrid or Remote.</summary>
    public string WorkMode { get; set; } = string.Empty;

    /// <summary>Lower bound of the salary, if known.</summary>
    public decimal? SalaryMin { get; set; }

    /// <summary>Upper bound of the salary, if known.</summary>
    public decimal? SalaryMax { get; set; }

    /// <summary>Whether the user has a referral.</summary>
    public bool HadReferral { get; set; }

    /// <summary>Whether the user will tailor their resume.</summary>
    public bool TailoredResume { get; set; }

    /// <summary>Whether the user will write a cover letter.</summary>
    public bool WroteCoverLetter { get; set; }
}
