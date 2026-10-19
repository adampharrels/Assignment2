using JobTracker.Analytics.MachineLearning;
using JobTracker.Core.Analytics;

namespace JobTracker.Tests.Analytics.MachineLearning;

[TestFixture]
public class ClassifierTests
{
    /// <summary>Feature 0 decides the label; feature 1 is noise. Any sensible model should learn this.</summary>
    private static (List<double[]> Features, List<bool> Labels) SeparableData()
    {
        var features = new List<double[]>();
        var labels = new List<bool>();
        for (int i = 0; i < 40; i++)
        {
            bool yes = i % 2 == 0;
            features.Add(new[] { yes ? 1.0 : 0.0, i % 3 == 0 ? 1.0 : 0.0 });
            labels.Add(yes);
        }

        return (features, labels);
    }

    private static IEnumerable<IClassifier> RealModels()
    {
        yield return new LogisticRegressionClassifier();
        yield return new NaiveBayesClassifier();
    }

    [TestCaseSource(nameof(RealModels))]
    public void Train_SeparableData_PredictsBothClassesCorrectly(IClassifier model)
    {
        (List<double[]> features, List<bool> labels) = SeparableData();

        model.Train(features, labels);

        // Not near 0/1: regularisation and smoothing deliberately keep both models from being overconfident.
        Assert.That(model.PredictProbability(new[] { 1.0, 0.0 }), Is.GreaterThan(0.7), model.Name);
        Assert.That(model.PredictProbability(new[] { 0.0, 0.0 }), Is.LessThan(0.3), model.Name);
    }

    [Test]
    public void LogisticRegression_LearnsPositiveWeightForTheDecidingFeature()
    {
        (List<double[]> features, List<bool> labels) = SeparableData();
        var model = new LogisticRegressionClassifier();

        model.Train(features, labels);

        Assert.That(model.Weights[0], Is.GreaterThan(1.0));
        Assert.That(Math.Abs(model.Weights[1]), Is.LessThan(model.Weights[0]));
    }

    [Test]
    public void Baseline_AlwaysPredictsTheTrainingRate()
    {
        var model = new MajorityBaselineClassifier();

        model.Train(new List<double[]> { new[] { 0.0 }, new[] { 1.0 }, new[] { 1.0 }, new[] { 0.0 } },
            new List<bool> { true, false, false, false });

        Assert.That(model.PredictProbability(new[] { 1.0 }), Is.EqualTo(0.25));
    }

    [Test]
    public void Train_MismatchedLabels_Throws()
    {
        var model = new LogisticRegressionClassifier();

        Assert.Throws<ArgumentException>(() => model.Train(new List<double[]> { new[] { 1.0 } }, new List<bool>()));
    }

    [Test]
    public void Predict_BeforeTraining_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new NaiveBayesClassifier().PredictProbability(new[] { 1.0 }));
    }

    [Test]
    public void Sigmoid_IsHalfAtZero()
    {
        Assert.That(LogisticRegressionClassifier.Sigmoid(0), Is.EqualTo(0.5));
    }

    [Test]
    public void CrossValidator_SeparableData_BeatsBaseline()
    {
        (List<double[]> features, List<bool> labels) = SeparableData();

        ModelEvaluation model = CrossValidator.Evaluate(() => new LogisticRegressionClassifier(), features, labels);
        ModelEvaluation baseline = CrossValidator.Evaluate(() => new MajorityBaselineClassifier(), features, labels);

        Assert.That(model.SampleSize, Is.EqualTo(40));
        Assert.That(model.Accuracy, Is.EqualTo(1.0));
        Assert.That(model.LogLoss, Is.LessThan(baseline.LogLoss));
        Assert.That(baseline.Recall, Is.EqualTo(0).Or.EqualTo(1));
    }

    [Test]
    public void CrossValidator_FewerExamplesThanFolds_StillTestsEveryExample()
    {
        var features = new List<double[]> { new[] { 1.0 }, new[] { 0.0 }, new[] { 1.0 } };
        var labels = new List<bool> { true, false, true };

        ModelEvaluation evaluation = CrossValidator.Evaluate(() => new NaiveBayesClassifier(), features, labels, folds: 10);

        Assert.That(evaluation.SampleSize, Is.EqualTo(3));
    }
}
