using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.Insights;

/// <summary>Recommends the most valuable missing skill to learn next.</summary>
public class SkillGapInsightGenerator : InsightGenerator
{
    /// <inheritdoc />
    protected override IEnumerable<Insight> CreateInsights(InsightContext context)
    {
        SkillGap? top = context.SkillGaps.FirstOrDefault();
        if (top == null || top.DocumentShare < 0.15)
        {
            yield break;
        }

        int jobsAsking = (int)Math.Round(top.DocumentShare * context.Records.Count);
        yield return new Insight
        {
            Title = $"Learn {top.Skill} next",
            Detail = top.Reason,
            Kind = InsightKind.Recommendation,
            Importance = top.Score * Math.Sqrt(context.Records.Count),
            SampleSize = jobsAsking
        };
    }
}
