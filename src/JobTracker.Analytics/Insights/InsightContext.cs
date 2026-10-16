using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.Insights;

/// <summary>Everything the insight generators may look at, calculated once and shared by all of them.</summary>
public class InsightContext
{
    /// <summary>All submitted applications.</summary>
    public IReadOnlyList<ApplicationRecord> Records { get; set; } = new List<ApplicationRecord>();

    /// <summary>Headline numbers.</summary>
    public PipelineSummary Summary { get; set; } = new PipelineSummary();

    /// <summary>Interview rate per source.</summary>
    public IReadOnlyList<GroupRate> SourceRates { get; set; } = new List<GroupRate>();

    /// <summary>Interview rate with and without each tactic (referral, tailored resume, ...).</summary>
    public IReadOnlyList<GroupRate> TacticRates { get; set; } = new List<GroupRate>();

    /// <summary>Interview rate per requested skill.</summary>
    public IReadOnlyList<GroupRate> SkillRates { get; set; } = new List<GroupRate>();

    /// <summary>Missing skills worth learning, best first.</summary>
    public IReadOnlyList<SkillGap> SkillGaps { get; set; } = new List<SkillGap>();
}
