namespace JobTracker.Core.Analytics;

/// <summary>How an application ended, separately from how far it got.</summary>
public enum ApplicationOutcome
{
    /// <summary>Still waiting or still in progress.</summary>
    InProgress = 0,
    /// <summary>The company said no.</summary>
    Rejected = 1,
    /// <summary>The user pulled out.</summary>
    Withdrawn = 2,
    /// <summary>No reply for a long time.</summary>
    Ghosted = 3,
    /// <summary>The user accepted an offer.</summary>
    Accepted = 4
}
