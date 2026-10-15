namespace JobTracker.Analytics.MachineLearning;

/// <summary>
/// The simplest possible "model": it ignores the features and always predicts the training
/// interview rate. A real model is only useful if it beats this.
/// </summary>
public class MajorityBaselineClassifier : IClassifier
{
    private double _positiveRate;

    /// <inheritdoc />
    public string Name => "Baseline (always the average)";

    /// <inheritdoc />
    public void Train(IReadOnlyList<double[]> features, IReadOnlyList<bool> labels)
    {
        TrainingData.Validate(features, labels);
        _positiveRate = labels.Count(label => label) / (double)labels.Count;
    }

    /// <inheritdoc />
    public double PredictProbability(double[] features)
    {
        return _positiveRate;
    }
}
