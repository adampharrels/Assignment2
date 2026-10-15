namespace JobTracker.Core.Enums;

/// <summary>The recorded result of an interview.</summary>
public enum InterviewOutcome
{
    /// <summary>No result has been recorded yet.</summary>
    Pending = 0,
    /// <summary>The applicant passed the interview.</summary>
    Passed = 1,
    /// <summary>The applicant did not pass the interview.</summary>
    Failed = 2,
    /// <summary>The interview was cancelled.</summary>
    Cancelled = 3
}
