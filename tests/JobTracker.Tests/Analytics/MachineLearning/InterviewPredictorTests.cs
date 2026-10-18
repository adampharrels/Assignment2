using JobTracker.Analytics.Demo;
using JobTracker.Analytics.MachineLearning;
using JobTracker.Analytics.Text;
using JobTracker.Core.Analytics;

namespace JobTracker.Tests.Analytics.MachineLearning;

[TestFixture]
public class InterviewPredictorTests
{
    private static readonly string[] ProfileSkills = { "c#", ".net", "react", "python", "sql" };
    private const string Resume = "Graduate developer with C#, .NET, React, Python and SQL experience.";

    private static InterviewPredictor TrainOnDemoData()
    {
        return new InterviewPredictor(DemoApplicationDataSource.Generate(), Resume, ProfileSkills, new DictionarySkillExtractor());
    }

    [Test]
    public void Predict_ReturnsProbabilityFactorsAndSkills()
    {
        InterviewPredictor predictor = TrainOnDemoData();
        ApplicationRecord record = TestRecords.Make(description: "C#, .NET and Kubernetes. Strong teamwork.");
        record.TailoredResume = true;

        InterviewPrediction prediction = predictor.Predict(record);

        Assert.That(predictor.IsTrained, Is.True);
        Assert.That(prediction.Probability, Is.InRange(0.0, 1.0));
        Assert.That(prediction.TopFactors, Has.Count.LessThanOrEqualTo(3).And.Count.GreaterThan(0));
        Assert.That(prediction.MatchedSkills, Is.EquivalentTo(new[] { "c#", ".net" }));
        Assert.That(prediction.MissingSkills, Is.EqualTo(new[] { "kubernetes" }), "Soft skills are not listed as missing.");
    }

    [Test]
    public void Predict_TailoredResumeRaisesTheChance()
    {
        InterviewPredictor predictor = TrainOnDemoData();
        ApplicationRecord generic = TestRecords.Make(description: "React and TypeScript");
        ApplicationRecord tailored = TestRecords.Make(description: "React and TypeScript");
        tailored.TailoredResume = true;

        Assert.That(predictor.Predict(tailored).Probability, Is.GreaterThan(predictor.Predict(generic).Probability));
    }

    [Test]
    public void Evaluate_ComparesThreeModels()
    {
        List<ModelEvaluation> evaluations = TrainOnDemoData().Evaluate();

        Assert.That(evaluations.Select(evaluation => evaluation.ModelName),
            Is.EquivalentTo(new[] { "Logistic regression", "Naive Bayes", "Baseline (always the average)" }));
    }

    [Test]
    public void Weights_HaveOneEntryPerFeature()
    {
        List<FeatureContribution> weights = TrainOnDemoData().Weights();

        Assert.That(weights.Select(weight => weight.Feature), Does.Contain("Referral"));
        Assert.That(weights.Select(weight => weight.Feature), Is.Unique);
    }

    [Test]
    public void TooLittleData_IsNotTrainedAndUsesAverage()
    {
        var records = new List<ApplicationRecord>
        {
            TestRecords.Make(PipelineStage.Interview, ApplicationOutcome.Rejected),
            TestRecords.Make(PipelineStage.Applied, ApplicationOutcome.Rejected)
        };

        var predictor = new InterviewPredictor(records, Resume, ProfileSkills, new DictionarySkillExtractor());

        Assert.That(predictor.IsTrained, Is.False);
        Assert.That(predictor.Predict(records[0]).Probability, Is.EqualTo(0.5));
        Assert.That(predictor.Evaluate(), Is.Empty);
    }

    [Test]
    public void IsDecided_RecentApplicationWithNoReply_IsNotDecided()
    {
        var today = new DateTime(2026, 9, 30);

        Assert.That(InterviewPredictor.IsDecided(TestRecords.Make(applied: today.AddDays(-5)), today), Is.False);
        Assert.That(InterviewPredictor.IsDecided(TestRecords.Make(applied: today.AddDays(-30)), today), Is.True);
        Assert.That(InterviewPredictor.IsDecided(TestRecords.Make(PipelineStage.Interview, applied: today), today), Is.True);
    }

    [Test]
    public void FeatureBuilder_RowMatchesFeatureNames()
    {
        var builder = new FeatureBuilder(new DictionarySkillExtractor());
        IReadOnlyList<ApplicationRecord> records = DemoApplicationDataSource.Generate();

        builder.Fit(records, Resume, ProfileSkills);

        Assert.That(builder.Build(records[0]).Length, Is.EqualTo(builder.FeatureNames.Count));
        Assert.That(builder.ResumeFit(Resume), Is.GreaterThan(builder.ResumeFit("Kubernetes Terraform Linux")));
    }
}
