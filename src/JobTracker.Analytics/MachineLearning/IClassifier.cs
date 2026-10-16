namespace JobTracker.Analytics.MachineLearning;

/// <summary>
/// A yes/no classifier: "will this application reach an interview?". Every model implements the
/// same two methods, so the cross-validator can compare them without knowing which one it has.
/// </summary>
public interface IClassifier
{
    /// <summary>Display name, e.g. "Logistic regression".</summary>
    string Name { get; }

    /// <summary>Learns from examples. Each row of features lines up with one label.</summary>
    void Train(IReadOnlyList<double[]> features, IReadOnlyList<bool> labels);

    /// <summary>The probability (0-1) that the example is a "yes".</summary>
    double PredictProbability(double[] features);
}
