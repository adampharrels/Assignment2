namespace JobTracker.Core.Analytics;

/// <summary>An in-demand skill the user does not have yet, ranked by how much it is likely to help.</summary>
public class SkillGap
{
    /// <summary>The canonical skill name.</summary>
    public string Skill { get; set; } = string.Empty;

    /// <summary>Share of job descriptions that ask for it, between 0 and 1.</summary>
    public double DocumentShare { get; set; }

    /// <summary>Interview-rate lift of applications that ask for it (1 = average).</summary>
    public double InterviewLift { get; set; }

    /// <summary>Ranking score: demand weighted by lift. Higher means learn it sooner.</summary>
    public double Score { get; set; }

    /// <summary>A one-line plain-English explanation.</summary>
    public string Reason { get; set; } = string.Empty;
}
