namespace JobTracker.Core.Analytics;

/// <summary>
/// The furthest hiring stage an application reached. The order matters: analytics asks
/// "did this reach at least Interview?" by comparing values.
/// </summary>
public enum PipelineStage
{
    /// <summary>Submitted, no progress yet.</summary>
    Applied = 0,
    /// <summary>Online test, coding challenge or video screen.</summary>
    Assessment = 1,
    /// <summary>At least one interview.</summary>
    Interview = 2,
    /// <summary>Final round interview or assessment centre.</summary>
    FinalRound = 3,
    /// <summary>An offer was made.</summary>
    Offer = 4
}
