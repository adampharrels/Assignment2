using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.Insights;

/// <summary>
/// Base class for everything that turns statistics into plain-English findings. Each subclass looks
/// at one topic (sources, tactics, skills, timing, skill gaps) and overrides CreateInsights.
/// The base class enforces the shared rule: say nothing when there is too little data to be honest.
/// </summary>
public abstract class InsightGenerator
{
    /// <summary>Below this many applications the generator stays silent. Subclasses may raise it.</summary>
    protected virtual int MinimumApplications => 10;

    /// <summary>Returns this generator's insights, or none when there is not enough data.</summary>
    public IEnumerable<Insight> Generate(InsightContext context)
    {
        if (context.Records.Count < MinimumApplications)
        {
            return Enumerable.Empty<Insight>();
        }

        return CreateInsights(context);
    }

    /// <summary>Produces the insights for this generator's topic.</summary>
    protected abstract IEnumerable<Insight> CreateInsights(InsightContext context);

    /// <summary>
    /// Shared importance score for "group vs average" findings: the size of the effect times the
    /// square root of the sample, so a big effect on many applications beats a big effect on few.
    /// </summary>
    protected static double EffectImportance(double lift, int sampleSize)
    {
        return Math.Abs(lift - 1) * Math.Sqrt(sampleSize);
    }

    /// <summary>"9 of 15 (60%)" style text.</summary>
    protected static string Describe(GroupRate rate)
    {
        return $"{rate.Successes} of {rate.Total} ({rate.Rate:P0}, 95% CI {rate.CiLow:P0}-{rate.CiHigh:P0})";
    }
}
