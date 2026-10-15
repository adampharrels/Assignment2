using JobTracker.Analytics.Statistics;
using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.Insights;

/// <summary>Finds the skills whose jobs convert best for the user.</summary>
public class SkillInsightGenerator : InsightGenerator
{
    /// <inheritdoc />
    protected override IEnumerable<Insight> CreateInsights(InsightContext context)
    {
        GroupRate? best = context.SkillRates
            .Where(rate => rate.IsReliable && rate.Lift >= 1.4)
            .OrderByDescending(rate => rate.Lift)
            .FirstOrDefault();
        if (best != null)
        {
            yield return new Insight
            {
                Title = $"Jobs asking for {best.Group} are your strongest match",
                Detail = $"{Describe(best)} reached an interview ({TextFormat.Times(best.Lift)} average). Prioritise similar roles.",
                Kind = InsightKind.Positive,
                Importance = EffectImportance(best.Lift, best.Total),
                SampleSize = best.Total
            };
        }

        GroupRate? worst = context.SkillRates
            .Where(rate => rate.IsReliable && rate.Lift <= 0.6)
            .OrderBy(rate => rate.Lift)
            .FirstOrDefault();
        if (worst != null)
        {
            yield return new Insight
            {
                Title = $"Jobs asking for {worst.Group} rarely convert",
                Detail = $"Only {Describe(worst)} reached an interview ({TextFormat.Times(worst.Lift)} average).",
                Kind = InsightKind.Negative,
                Importance = EffectImportance(worst.Lift, worst.Total),
                SampleSize = worst.Total
            };
        }
    }
}
