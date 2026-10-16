namespace JobTracker.Analytics.MachineLearning;

/// <summary>Shared input checks for every classifier's Train method.</summary>
internal static class TrainingData
{
    /// <summary>Throws ArgumentException unless there is at least one example and every row has the same length.</summary>
    public static void Validate(IReadOnlyList<double[]> features, IReadOnlyList<bool> labels)
    {
        if (features.Count == 0)
        {
            throw new ArgumentException("At least one training example is needed.", nameof(features));
        }

        if (features.Count != labels.Count)
        {
            throw new ArgumentException("Every feature row needs exactly one label.", nameof(labels));
        }

        int featureCount = features[0].Length;
        if (features.Any(row => row.Length != featureCount))
        {
            throw new ArgumentException("Every feature row must have the same number of values.", nameof(features));
        }
    }
}
