using JobTracker.Analytics.Text;
using JobTracker.Core.Analytics;

namespace JobTracker.Tests.Analytics.Text;

[TestFixture]
public class SkillAnalysisTests
{
    private static SkillAnalysis CreateAnalysis()
    {
        var records = new List<ApplicationRecord>
        {
            TestRecords.Make(PipelineStage.Interview, description: "React and SQL"),
            TestRecords.Make(PipelineStage.Interview, description: "React and Python"),
            TestRecords.Make(PipelineStage.Applied, description: "Kubernetes and SQL"),
            TestRecords.Make(PipelineStage.Applied, description: "Kubernetes and Docker"),
            TestRecords.Make(PipelineStage.Applied, description: "Kubernetes and Java"),
            TestRecords.Make(PipelineStage.Applied, description: "Teamwork, teamwork, teamwork")
        };
        return new SkillAnalysis(records, new DictionarySkillExtractor(), new[] { "react", "sql", "python" });
    }

    [Test]
    public void Demand_RanksMostRequestedSkillsFirst()
    {
        List<SkillDemand> demand = CreateAnalysis().Demand(top: 3);

        Assert.That(demand[0].Skill, Is.EqualTo("kubernetes"));
        Assert.That(demand[0].DocumentCount, Is.EqualTo(3));
        Assert.That(demand[0].DocumentShare, Is.EqualTo(0.5));
        Assert.That(demand[0].InMyProfile, Is.False);
        Assert.That(demand.Single(item => item.Skill == "react").InMyProfile, Is.True);
    }

    [Test]
    public void Gap_ExcludesProfileSkillsAndRanksByDemand()
    {
        List<SkillGap> gap = CreateAnalysis().Gap(top: 5);

        Assert.That(gap.Select(item => item.Skill), Has.None.EqualTo("react"));
        Assert.That(gap.Select(item => item.Skill), Has.None.EqualTo("teamwork"));
        Assert.That(gap[0].Skill, Is.EqualTo("kubernetes"));
        Assert.That(gap[0].Reason, Does.Contain("50%"));
    }

    [Test]
    public void InterviewRates_GroupsBySkill()
    {
        List<GroupRate> rates = CreateAnalysis().InterviewRates();

        GroupRate react = rates.Single(rate => rate.Group == "react");
        Assert.That(react.Total, Is.EqualTo(2));
        Assert.That(react.Rate, Is.EqualTo(1.0));
    }

    [Test]
    public void Demand_NoApplications_ReturnsEmpty()
    {
        var analysis = new SkillAnalysis(new List<ApplicationRecord>(), new DictionarySkillExtractor(), Array.Empty<string>());

        Assert.That(analysis.Demand(10), Is.Empty);
        Assert.That(analysis.Gap(10), Is.Empty);
    }
}
