namespace JobTracker.Analytics.MachineLearning;

/// <summary>
/// Logistic regression trained with batch gradient descent, written from scratch.
/// It learns one weight per feature: the prediction is sigmoid(bias + sum of weight x value).
/// A positive weight means the feature raises the interview chance. L2 regularisation keeps the
/// weights small, which matters with only a few dozen applications to learn from.
/// </summary>
public class LogisticRegressionClassifier : IClassifier
{
    private readonly double _learningRate;
    private readonly int _epochs;
    private readonly double _l2Penalty;

    /// <summary>Creates an untrained model.</summary>
    /// <param name="learningRate">Step size for each gradient-descent update.</param>
    /// <param name="epochs">How many passes over the training data.</param>
    /// <param name="l2Penalty">Strength of the L2 penalty on the weights (not the bias).</param>
    public LogisticRegressionClassifier(double learningRate = 0.1, int epochs = 3000, double l2Penalty = 0.05)
    {
        _learningRate = learningRate;
        _epochs = epochs;
        _l2Penalty = l2Penalty;
    }

    /// <inheritdoc />
    public string Name => "Logistic regression";

    /// <summary>The learned weight of each feature, in feature order.</summary>
    public double[] Weights { get; private set; } = new double[0];

    /// <summary>The learned bias (log-odds when every feature is 0).</summary>
    public double Bias { get; private set; }

    /// <inheritdoc />
    public void Train(IReadOnlyList<double[]> features, IReadOnlyList<bool> labels)
    {
        TrainingData.Validate(features, labels);
        int sampleCount = features.Count;
        int featureCount = features[0].Length;
        Weights = new double[featureCount];
        Bias = 0;

        for (int epoch = 0; epoch < _epochs; epoch++)
        {
            var weightGradient = new double[featureCount];
            double biasGradient = 0;

            // Gradient of the average log loss: (prediction - label) x feature value.
            for (int sample = 0; sample < sampleCount; sample++)
            {
                double error = PredictProbability(features[sample]) - (labels[sample] ? 1.0 : 0.0);
                for (int feature = 0; feature < featureCount; feature++)
                {
                    weightGradient[feature] += error * features[sample][feature];
                }

                biasGradient += error;
            }

            for (int feature = 0; feature < featureCount; feature++)
            {
                double gradient = weightGradient[feature] / sampleCount + _l2Penalty * Weights[feature];
                Weights[feature] -= _learningRate * gradient;
            }

            Bias -= _learningRate * biasGradient / sampleCount;
        }
    }

    /// <inheritdoc />
    public double PredictProbability(double[] features)
    {
        return Sigmoid(Bias + Contributions(features).Sum());
    }

    /// <summary>Each feature's effect on the log-odds for this example: weight x value.</summary>
    public double[] Contributions(double[] features)
    {
        if (features.Length != Weights.Length)
        {
            throw new InvalidOperationException("Train the model before predicting, with the same number of features.");
        }

        var contributions = new double[features.Length];
        for (int i = 0; i < features.Length; i++)
        {
            contributions[i] = Weights[i] * features[i];
        }

        return contributions;
    }

    /// <summary>Squashes any number into the range 0-1.</summary>
    public static double Sigmoid(double value)
    {
        return 1.0 / (1.0 + Math.Exp(-value));
    }
}
