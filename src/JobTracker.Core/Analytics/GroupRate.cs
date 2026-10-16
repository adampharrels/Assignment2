namespace JobTracker.Core.Analytics;

/// <summary>
/// The interview rate of one group, e.g. applications asking for React, or applications from LinkedIn.
/// Comes with a 95% confidence interval, because with small numbers a difference can easily be luck.
/// </summary>
public class GroupRate
{
    /// <summary>Groups smaller than this are flagged as unreliable in the UI.</summary>
    public const int MinReliableSample = 5;

    /// <summary>The group's display name, e.g. "react" or "LinkedIn".</summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>Applications in the group.</summary>
    public int Total { get; set; }

    /// <summary>Applications in the group that reached an interview.</summary>
    public int Successes { get; set; }

    /// <summary>Successes / Total, between 0 and 1.</summary>
    public double Rate { get; set; }

    /// <summary>Lower bound of the 95% Wilson confidence interval.</summary>
    public double CiLow { get; set; }

    /// <summary>Upper bound of the 95% Wilson confidence interval.</summary>
    public double CiHigh { get; set; }

    /// <summary>Rate divided by the overall interview rate: 1.5 means 50% better than average.</summary>
    public double Lift { get; set; }

    /// <summary>True when the group is big enough to take seriously.</summary>
    public bool IsReliable => Total >= MinReliableSample;
}
