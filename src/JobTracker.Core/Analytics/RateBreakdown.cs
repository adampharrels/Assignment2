namespace JobTracker.Core.Analytics;

/// <summary>The ways interview rates can be broken down on the analytics page.</summary>
public enum RateBreakdown
{
    /// <summary>One group per skill mentioned in the job description.</summary>
    Skill = 0,
    /// <summary>One group per source (LinkedIn, Seek, Referral, ...).</summary>
    Source = 1,
    /// <summary>One group per location.</summary>
    Location = 2,
    /// <summary>One group per work mode (On-site, Hybrid, Remote).</summary>
    WorkMode = 3,
    /// <summary>One group per salary band.</summary>
    SalaryBand = 4,
    /// <summary>With vs without referral, tailored resume and cover letter.</summary>
    Tactics = 5
}
