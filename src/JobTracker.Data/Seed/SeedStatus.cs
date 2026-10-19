namespace JobTracker.Data.Seed;

/// <summary>
/// The application statuses from the spec's status rules, used by the seed stage histories.
/// Mirrors ApplicationStatus value for value (SeedData.ToEntity casts between them); the pipeline values are in forward order.
/// </summary>
public enum SeedStatus
{
    /// <summary>Saved for later, not yet submitted.</summary>
    Wishlist = 0,
    /// <summary>Submitted, no progress yet.</summary>
    Applied = 1,
    /// <summary>Online test, coding challenge or video screen.</summary>
    Assessment = 2,
    /// <summary>At least one interview.</summary>
    Interview = 3,
    /// <summary>Final round interview or assessment centre.</summary>
    FinalRound = 4,
    /// <summary>An offer was made.</summary>
    Offer = 5,
    /// <summary>The user accepted the offer. Terminal.</summary>
    Accepted = 6,
    /// <summary>The company said no. Terminal.</summary>
    Rejected = 7,
    /// <summary>The user pulled out. Terminal.</summary>
    Withdrawn = 8,
    /// <summary>No reply for a long time; can be revived if the company replies late.</summary>
    Ghosted = 9
}
