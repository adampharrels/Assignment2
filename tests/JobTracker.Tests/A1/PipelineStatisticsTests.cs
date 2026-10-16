using JobTracker.Analytics.Statistics;
using JobTracker.Core.Analytics;

namespace JobTracker.Tests.A1;

[TestFixture]
public class PipelineStatisticsTests
{
    private static List<ApplicationRecord> SampleRecords()
    {
        return new List<ApplicationRecord>
        {
            TestRecords.Make(PipelineStage.Offer, ApplicationOutcome.Accepted, daysToResponse: 4),
            TestRecords.Make(PipelineStage.Interview, ApplicationOutcome.Rejected, daysToResponse: 10),
            TestRecords.Make(PipelineStage.Applied, ApplicationOutcome.Rejected, daysToResponse: 20),
            TestRecords.Make(PipelineStage.Applied, ApplicationOutcome.Ghosted)
        };
    }

    [Test]
    public void Summarise_CountsEachStage()
    {
        PipelineSummary summary = PipelineStatistics.Summarise(SampleRecords());

        Assert.That(summary.TotalApplications, Is.EqualTo(4));
        Assert.That(summary.Responses, Is.EqualTo(3));
        Assert.That(summary.Interviews, Is.EqualTo(2));
        Assert.That(summary.Offers, Is.EqualTo(1));
        Assert.That(summary.InterviewRate, Is.EqualTo(0.5));
        Assert.That(summary.GhostingRate, Is.EqualTo(0.25));
        Assert.That(summary.MedianDaysToResponse, Is.EqualTo(10));
    }

    [Test]
    public void Summarise_NoApplications_ReturnsZeros()
    {
        PipelineSummary summary = PipelineStatistics.Summarise(new List<ApplicationRecord>());

        Assert.That(summary.TotalApplications, Is.EqualTo(0));
        Assert.That(summary.InterviewRate, Is.EqualTo(0));
        Assert.That(summary.MedianDaysToResponse, Is.Null);
    }

    [Test]
    public void BuildFunnel_ConversionIsRelativeToPreviousStage()
    {
        List<FunnelStage> funnel = PipelineStatistics.BuildFunnel(SampleRecords());

        Assert.That(funnel.Select(stage => stage.Count), Is.EqualTo(new[] { 4, 3, 2, 1, 1 }));
        Assert.That(funnel[2].ConversionFromPrevious, Is.EqualTo(2.0 / 3).Within(1e-9));
        Assert.That(funnel[4].ConversionFromStart, Is.EqualTo(0.25));
    }

    [Test]
    public void WeeklyTrend_FillsEmptyWeeksWithZero()
    {
        var records = new List<ApplicationRecord>
        {
            TestRecords.Make(applied: new DateTime(2026, 8, 3)),   // Monday, week 1
            TestRecords.Make(PipelineStage.Interview, applied: new DateTime(2026, 8, 7)),
            TestRecords.Make(applied: new DateTime(2026, 8, 19))   // Wednesday, week 3
        };

        List<TrendPoint> trend = PipelineStatistics.WeeklyTrend(records);

        Assert.That(trend, Has.Count.EqualTo(3));
        Assert.That(trend[0].Applications, Is.EqualTo(2));
        Assert.That(trend[0].Interviews, Is.EqualTo(1));
        Assert.That(trend[1].Applications, Is.EqualTo(0));
        Assert.That(trend[2].WeekStart, Is.EqualTo(new DateTime(2026, 8, 17)));
    }

    [Test]
    public void InterviewRates_Tactics_ComparesWithAndWithout()
    {
        var records = new List<ApplicationRecord>
        {
            TestRecords.Make(PipelineStage.Interview, referral: true),
            TestRecords.Make(PipelineStage.Applied, referral: false)
        };

        List<GroupRate> rates = PipelineStatistics.InterviewRates(records, RateBreakdown.Tactics);

        Assert.That(rates.Single(rate => rate.Group == "Referral").Rate, Is.EqualTo(1.0));
        Assert.That(rates.Single(rate => rate.Group == "No referral").Rate, Is.EqualTo(0.0));
    }

    [TestCase(70_000, 75_000, "Under $80k")]
    [TestCase(85_000, 95_000, "$90k-$100k")]
    [TestCase(110_000, null, "$100k+")]
    [TestCase(null, null, "Not stated")]
    public void SalaryBand_UsesMidpoint(double? min, double? max, string expected)
    {
        ApplicationRecord record = TestRecords.Make(salaryMin: (decimal?)min, salaryMax: (decimal?)max);

        Assert.That(record.SalaryBand(), Is.EqualTo(expected));
    }
}
