using JobTracker.Analytics.Statistics;
using JobTracker.Core.Analytics;

namespace JobTracker.Tests.Analytics.Statistics;

[TestFixture]
public class RateStatisticsTests
{
    [Test]
    public void WilsonInterval_HalfOfTen_MatchesKnownValues()
    {
        (double low, double high) = RateStatistics.WilsonInterval(5, 10);

        Assert.That(low, Is.EqualTo(0.2366).Within(0.0005));
        Assert.That(high, Is.EqualTo(0.7634).Within(0.0005));
    }

    [Test]
    public void WilsonInterval_ZeroSuccesses_StaysAboveZero()
    {
        (double low, double high) = RateStatistics.WilsonInterval(0, 10);

        Assert.That(low, Is.EqualTo(0));
        Assert.That(high, Is.GreaterThan(0).And.LessThan(0.35));
    }

    [Test]
    public void WilsonInterval_NoData_ReturnsZeros()
    {
        Assert.That(RateStatistics.WilsonInterval(0, 0), Is.EqualTo((0.0, 0.0)));
    }

    [Test]
    public void WilsonInterval_MoreSuccessesThanTotal_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RateStatistics.WilsonInterval(11, 10));
    }

    [Test]
    public void GroupRates_ItemInSeveralGroups_IsCountedInEach()
    {
        var items = new[]
        {
            (Skills: new[] { "react", "sql" }, Interview: true),
            (Skills: new[] { "react" }, Interview: false),
            (Skills: new[] { "sql" }, Interview: false),
            (Skills: new[] { "sql" }, Interview: false)
        };

        List<GroupRate> rates = RateStatistics.GroupRates(items, item => item.Skills, item => item.Interview);

        GroupRate react = rates.Single(rate => rate.Group == "react");
        GroupRate sql = rates.Single(rate => rate.Group == "sql");
        Assert.That(react.Total, Is.EqualTo(2));
        Assert.That(react.Rate, Is.EqualTo(0.5));
        Assert.That(sql.Total, Is.EqualTo(3));
        // Baseline is 1 in 4 = 0.25, so react's 0.5 is a lift of 2.
        Assert.That(react.Lift, Is.EqualTo(2.0).Within(1e-9));
    }

    [Test]
    public void GroupRates_SmallGroupsAreFlaggedAndSortedLast()
    {
        var items = Enumerable.Range(0, 6).Select(i => (Group: "big", Success: i < 2))
            .Append((Group: "small", Success: true))
            .ToList();

        List<GroupRate> rates = RateStatistics.GroupRates(items, item => item.Group, item => item.Success);

        Assert.That(rates[0].Group, Is.EqualTo("big"));
        Assert.That(rates[0].IsReliable, Is.True);
        Assert.That(rates[1].IsReliable, Is.False);
    }

    [TestCase(new double[] { 3, 1, 2 }, 2.0)]
    [TestCase(new double[] { 4, 1, 3, 2 }, 2.5)]
    public void Median_OddAndEvenCounts(double[] values, double expected)
    {
        Assert.That(RateStatistics.Median(values), Is.EqualTo(expected));
    }

    [Test]
    public void Median_Empty_ReturnsNull()
    {
        Assert.That(RateStatistics.Median(Array.Empty<double>()), Is.Null);
    }
}
