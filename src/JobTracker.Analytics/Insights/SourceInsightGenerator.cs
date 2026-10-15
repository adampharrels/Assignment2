using JobTracker.Analytics.Statistics;
using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.Insights;

/// <summary>Points out the best and worst places the user finds jobs.</summary>
public class SourceInsightGenerator : InsightGenerator
{
    /// <inheritdoc />
    protected override IEnumerable<Insight> CreateInsights(InsightContext context)
    {
        List<GroupRate> reliable = context.SourceRates.Where(rate => rate.IsReliable).ToList();
        if (reliable.Count < 2)
        {
            yield break;
        }

        GroupRate best = reliable.OrderByDescending(rate => rate.Lift).First();
        if (best.Lift >= 1.3)
        {
            yield return new Insight
            {
                Title = $"{best.Group} is your best source: {TextFormat.Times(best.Lift)} the average interview rate",
                Detail = $"{Describe(best)} reached an interview, vs {TextFormat.Percent(context.Summary.InterviewRate)} overall.",
                Kind = InsightKind.Positive,
                Importance = EffectImportance(best.Lift, best.Total),
                SampleSize = best.Total
            };
        }

        GroupRate worst = reliable.OrderBy(rate => rate.Lift).First();
        if (worst.Lift <= 0.7)
        {
            yield return new Insight
            {
                Title = $"{worst.Group} applications rarely lead to interviews",
                Detail = $"Only {Describe(worst)} reached an interview, vs {TextFormat.Percent(context.Summary.InterviewRate)} overall. "
                    + "Consider spending that time on better-performing sources.",
                Kind = InsightKind.Negative,
                Importance = EffectImportance(worst.Lift, worst.Total),
                SampleSize = worst.Total
            };
        }
    }
}
