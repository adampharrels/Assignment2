using JobTracker.Analytics.Statistics;
using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.Insights;

/// <summary>Reports how long companies take to reply and how often the user is ghosted.</summary>
public class TimingInsightGenerator : InsightGenerator
{
    private const double HighGhostingRate = 0.25;

    /// <inheritdoc />
    protected override IEnumerable<Insight> CreateInsights(InsightContext context)
    {
        PipelineSummary summary = context.Summary;

        if (summary.MedianDaysToResponse.HasValue)
        {
            int followUpDay = (int)Math.Ceiling(summary.MedianDaysToResponse.Value) + 7;
            yield return new Insight
            {
                Title = $"Companies reply in a median of {summary.MedianDaysToResponse.Value:0} days",
                Detail = $"Based on {summary.Responses} replies. If you hear nothing after about {followUpDay} days, send a polite follow-up.",
                Kind = InsightKind.Neutral,
                Importance = 0.5 * Math.Sqrt(summary.Responses),
                SampleSize = summary.Responses
            };
        }

        if (summary.GhostingRate >= HighGhostingRate)
        {
            int ghosted = context.Records.Count(record => record.Outcome == ApplicationOutcome.Ghosted);
            yield return new Insight
            {
                Title = $"{TextFormat.Percent(summary.GhostingRate)} of your applications were ghosted",
                Detail = $"{ghosted} of {summary.TotalApplications} never got a reply. Applying through referrals or "
                    + "company websites usually gets more answers than job boards.",
                Kind = InsightKind.Negative,
                Importance = (summary.GhostingRate - 0.15) * 2 * Math.Sqrt(summary.TotalApplications),
                SampleSize = summary.TotalApplications
            };
        }
    }
}
