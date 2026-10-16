using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.Insights;

/// <summary>Runs every insight generator and ranks what they find.</summary>
public class InsightEngine
{
    private readonly List<InsightGenerator> _generators;

    /// <summary>Creates the engine with all built-in generators.</summary>
    public InsightEngine()
        : this(new List<InsightGenerator>
        {
            new SourceInsightGenerator(),
            new TacticsInsightGenerator(),
            new SkillInsightGenerator(),
            new TimingInsightGenerator(),
            new SkillGapInsightGenerator()
        })
    {
    }

    /// <summary>Creates the engine with a custom set of generators (used by tests).</summary>
    public InsightEngine(List<InsightGenerator> generators)
    {
        _generators = generators;
    }

    /// <summary>All insights from all generators, most important first.</summary>
    public List<Insight> Generate(InsightContext context)
    {
        return _generators
            .SelectMany(generator => generator.Generate(context))
            .OrderByDescending(insight => insight.Importance)
            .ToList();
    }
}
