using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.Insights;

/// <summary>Checks whether referrals, tailored resumes, cover letters and applying early pay off.</summary>
public class TacticsInsightGenerator : InsightGenerator
{
    // Each tactic: (group with it, group without it, advice when it helps).
    private static readonly (string With, string Without, string Advice)[] Tactics =
    {
        ("Referral", "No referral", "Ask for referrals"),
        ("Tailored resume", "Generic resume", "Tailor your resume"),
        ("Cover letter", "No cover letter", "Write a cover letter"),
        ("Applied within 3 days", "Applied later", "Apply early")
    };

    /// <inheritdoc />
    protected override IEnumerable<Insight> CreateInsights(InsightContext context)
    {
        Dictionary<string, GroupRate> rates = context.TacticRates.ToDictionary(rate => rate.Group);

        foreach ((string withName, string withoutName, string advice) in Tactics)
        {
            if (!rates.TryGetValue(withName, out GroupRate? with) || !rates.TryGetValue(withoutName, out GroupRate? without))
            {
                continue;
            }

            if (!with.IsReliable || !without.IsReliable || without.Rate == 0)
            {
                continue;
            }

            double ratio = with.Rate / without.Rate;
            int sampleSize = with.Total + without.Total;
            if (ratio >= 1.3)
            {
                yield return new Insight
                {
                    Title = $"{advice}: {ratio:0.0}x the interview rate",
                    Detail = $"{withName}: {Describe(with)}. {withoutName}: {Describe(without)}.",
                    Kind = InsightKind.Recommendation,
                    Importance = EffectImportance(ratio, sampleSize),
                    SampleSize = sampleSize
                };
            }
            else if (ratio <= 0.77)
            {
                yield return new Insight
                {
                    Title = $"\"{withName}\" is not paying off so far",
                    Detail = $"{withName}: {Describe(with)}. {withoutName}: {Describe(without)}. "
                        + "The difference may be luck; keep watching it.",
                    Kind = InsightKind.Neutral,
                    Importance = EffectImportance(ratio, sampleSize) * 0.5,
                    SampleSize = sampleSize
                };
            }
        }
    }
}
