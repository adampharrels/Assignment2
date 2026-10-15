namespace JobTracker.Core.Analytics;

/// <summary>How much employers ask for a skill across all stored job descriptions.</summary>
public class SkillDemand
{
    /// <summary>The canonical skill name.</summary>
    public string Skill { get; set; } = string.Empty;

    /// <summary>Number of job descriptions that mention the skill.</summary>
    public int DocumentCount { get; set; }

    /// <summary>DocumentCount / number of job descriptions, between 0 and 1.</summary>
    public double DocumentShare { get; set; }

    /// <summary>Average TF-IDF weight of the skill in the descriptions that mention it.</summary>
    public double AverageWeight { get; set; }

    /// <summary>True when the user already has the skill.</summary>
    public bool InMyProfile { get; set; }
}
