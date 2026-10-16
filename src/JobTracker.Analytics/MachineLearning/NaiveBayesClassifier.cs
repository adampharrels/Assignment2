namespace JobTracker.Analytics.MachineLearning;

/// <summary>
/// Bernoulli naive Bayes, written from scratch. Each feature is treated as on (value above 0) or off.
/// It learns how often each feature is on among applications that did and did not get an interview,
/// then combines those odds assuming the features are independent ("naive").
/// Laplace smoothing (+1 / +2) stops a feature never seen in one class from zeroing the probability.
/// </summary>
public class NaiveBayesClassifier : IClassifier
{
    private double _logPriorYes;
    private double _logPriorNo;
    private double[] _probabilityOnGivenYes = new double[0];
    private double[] _probabilityOnGivenNo = new double[0];

    /// <inheritdoc />
    public string Name => "Naive Bayes";

    /// <inheritdoc />
    public void Train(IReadOnlyList<double[]> features, IReadOnlyList<bool> labels)
    {
        TrainingData.Validate(features, labels);
        int featureCount = features[0].Length;
        int yesCount = labels.Count(label => label);
        int noCount = labels.Count - yesCount;

        _logPriorYes = Math.Log((yesCount + 1.0) / (labels.Count + 2.0));
        _logPriorNo = Math.Log((noCount + 1.0) / (labels.Count + 2.0));
        _probabilityOnGivenYes = new double[featureCount];
        _probabilityOnGivenNo = new double[featureCount];

        for (int feature = 0; feature < featureCount; feature++)
        {
            int onAndYes = 0;
            int onAndNo = 0;
            for (int sample = 0; sample < features.Count; sample++)
            {
                if (!IsOn(features[sample][feature]))
                {
                    continue;
                }

                if (labels[sample])
                {
                    onAndYes++;
                }
                else
                {
                    onAndNo++;
                }
            }

            _probabilityOnGivenYes[feature] = (onAndYes + 1.0) / (yesCount + 2.0);
            _probabilityOnGivenNo[feature] = (onAndNo + 1.0) / (noCount + 2.0);
        }
    }

    /// <inheritdoc />
    public double PredictProbability(double[] features)
    {
        if (features.Length != _probabilityOnGivenYes.Length)
        {
            throw new InvalidOperationException("Train the model before predicting, with the same number of features.");
        }

        // Work with log probabilities so multiplying many small numbers does not underflow to 0.
        double logYes = _logPriorYes;
        double logNo = _logPriorNo;
        for (int feature = 0; feature < features.Length; feature++)
        {
            bool on = IsOn(features[feature]);
            logYes += Math.Log(on ? _probabilityOnGivenYes[feature] : 1 - _probabilityOnGivenYes[feature]);
            logNo += Math.Log(on ? _probabilityOnGivenNo[feature] : 1 - _probabilityOnGivenNo[feature]);
        }

        return 1.0 / (1.0 + Math.Exp(logNo - logYes));
    }

    /// <summary>Yes/no features are 0 or 1; scaled numeric features count as on when above average.</summary>
    private static bool IsOn(double value)
    {
        return value > 0;
    }
}
