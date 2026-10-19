using JobTracker.Analytics.Demo;
using JobTracker.Core.Analytics;

namespace JobTracker.Tests.Analytics.Demo;

[TestFixture]
public class DemoApplicationDataSourceTests
{
    [Test]
    public void Generate_ProducesAtLeastFiftyApplications()
    {
        Assert.That(DemoApplicationDataSource.Generate(), Has.Count.GreaterThanOrEqualTo(50));
    }

    [Test]
    public void Generate_IsTheSameEveryTime()
    {
        IReadOnlyList<ApplicationRecord> first = DemoApplicationDataSource.Generate();
        IReadOnlyList<ApplicationRecord> second = DemoApplicationDataSource.Generate();

        Assert.That(second.Select(r => r.JobDescription), Is.EqualTo(first.Select(r => r.JobDescription)));
        Assert.That(second.Select(r => r.HighestStage), Is.EqualTo(first.Select(r => r.HighestStage)));
    }

    [Test]
    public void Generate_DatesAreConsistent()
    {
        foreach (ApplicationRecord record in DemoApplicationDataSource.Generate())
        {
            Assert.That(record.DatePosted, Is.LessThanOrEqualTo(record.DateApplied));
            if (record.DateFirstResponse.HasValue)
            {
                Assert.That(record.DateFirstResponse.Value, Is.GreaterThan(record.DateApplied));
            }
        }
    }

    [Test]
    public void Generate_HasAMixOfOutcomes()
    {
        IReadOnlyList<ApplicationRecord> records = DemoApplicationDataSource.Generate();

        Assert.That(records.Any(r => r.HighestStage >= PipelineStage.Interview), Is.True);
        Assert.That(records.Any(r => r.Outcome == ApplicationOutcome.Ghosted), Is.True);
        Assert.That(records.Any(r => r.Outcome == ApplicationOutcome.Rejected), Is.True);
    }
}
