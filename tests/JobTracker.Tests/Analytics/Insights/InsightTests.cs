using JobTracker.Analytics.Insights;
using JobTracker.Analytics.Statistics;
using JobTracker.Core.Analytics;

namespace JobTracker.Tests.Analytics.Insights;

[TestFixture]
public class InsightTests
{
    /// <summary>20 applications: referrals get interviews 8 times in 10, the rest 2 times in 10.</summary>
    private static InsightContext ReferralContext()
    {
        var records = new List<ApplicationRecord>();
        for (int i = 0; i < 10; i++)
        {
            records.Add(TestRecords.Make(i < 8 ? PipelineStage.Interview : PipelineStage.Applied, source: "Referral", referral: true, daysToResponse: 7));
            records.Add(TestRecords.Make(i < 2 ? PipelineStage.Interview : PipelineStage.Applied, ApplicationOutcome.Ghosted, source: "Seek"));
        }

        return new InsightContext
        {
            Records = records,
            Summary = PipelineStatistics.Summarise(records),
            SourceRates = PipelineStatistics.InterviewRates(records, RateBreakdown.Source),
            TacticRates = PipelineStatistics.InterviewRates(records, RateBreakdown.Tactics)
        };
    }

    [Test]
    public void TacticsGenerator_RecommendsReferralsWithSampleSize()
    {
        List<Insight> insights = new TacticsInsightGenerator().Generate(ReferralContext()).ToList();

        Insight referral = insights.Single(insight => insight.Title.StartsWith("Ask for referrals"));
        Assert.That(referral.Kind, Is.EqualTo(InsightKind.Recommendation));
        Assert.That(referral.Title, Does.Contain("4.0x"));
        Assert.That(referral.SampleSize, Is.EqualTo(20));
        Assert.That(referral.Detail, Does.Contain("8 of 10"));
    }

    [Test]
    public void SourceGenerator_FlagsBestAndWorstSources()
    {
        List<Insight> insights = new SourceInsightGenerator().Generate(ReferralContext()).ToList();

        Assert.That(insights.Select(insight => insight.Kind), Is.EquivalentTo(new[] { InsightKind.Positive, InsightKind.Negative }));
        Assert.That(insights.Single(insight => insight.Kind == InsightKind.Negative).Title, Does.StartWith("Seek"));
    }

    [Test]
    public void TimingGenerator_ReportsGhostingAndReplyTime()
    {
        List<Insight> insights = new TimingInsightGenerator().Generate(ReferralContext()).ToList();

        Assert.That(insights.Any(insight => insight.Title.Contains("ghosted")), Is.True);
        Assert.That(insights.Any(insight => insight.Title.Contains("median of 7 days")), Is.True);
    }

    [Test]
    public void Generators_StaySilentWithTooLittleData()
    {
        var context = new InsightContext
        {
            Records = new List<ApplicationRecord> { TestRecords.Make(PipelineStage.Interview, referral: true) },
            Summary = new PipelineSummary { GhostingRate = 1, MedianDaysToResponse = 3, Responses = 1 }
        };

        Assert.That(new InsightEngine().Generate(context), Is.Empty);
    }

    [Test]
    public void SkillGapGenerator_RecommendsTopGap()
    {
        InsightContext context = ReferralContext();
        context.SkillGaps = new List<SkillGap> { new SkillGap { Skill = "aws", DocumentShare = 0.4, Score = 0.5, Reason = "Asked for a lot." } };

        Insight insight = new SkillGapInsightGenerator().Generate(context).Single();

        Assert.That(insight.Title, Is.EqualTo("Learn aws next"));
        Assert.That(insight.SampleSize, Is.EqualTo(8));
    }

    [Test]
    public void Engine_SortsByImportance()
    {
        List<Insight> insights = new InsightEngine().Generate(ReferralContext());

        Assert.That(insights, Is.Not.Empty);
        Assert.That(insights.Select(insight => insight.Importance), Is.Ordered.Descending);
    }
}
