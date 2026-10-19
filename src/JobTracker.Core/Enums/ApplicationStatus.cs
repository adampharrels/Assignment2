namespace JobTracker.Core.Enums;

/// <summary>
/// Where a job application currently is. The pipeline values are in forward order, so a move
/// "forward" is a move to a higher value. The names and numbers match the seed data's statuses.
/// </summary>
/// <remarks>
/// Pipeline: Wishlist → Applied → Assessment → Interview → FinalRound → Offer → Accepted.
/// Rejected, Withdrawn and Ghosted are exits from the pipeline. See ApplicationStatusRules for
/// which moves are allowed.
/// </remarks>
public enum ApplicationStatus
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
