using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.MachineLearning;

/// <summary>
/// Measures how well a model predicts applications it has NOT seen, using stratified k-fold
/// cross-validation: split the data into k parts with the same yes/no mix, train on k-1 parts,
/// test on the remaining part, and repeat so every application is tested exactly once.
/// </summary>
public static class CrossValidator
{
    /// <summary>Probabilities are clipped to this range so one confident mistake cannot make log loss infinite.</summary>
    private const double ProbabilityFloor = 1e-6;

    /// <summary>Cross-validates a model. A new model is created for every fold.</summary>
    /// <param name="createModel">Makes a fresh, untrained model (so folds do not share learning).</param>
    /// <param name="features">One feature row per application.</param>
    /// <param name="labels">Whether each application reached an interview.</param>
    /// <param name="folds">Number of folds; reduced automatically when there are fewer examples.</param>
    public static ModelEvaluation Evaluate(Func<IClassifier> createModel, IReadOnlyList<double[]> features,
        IReadOnlyList<bool> labels, int folds = 5)
    {
        TrainingData.Validate(features, labels);
        int sampleCount = features.Count;
        folds = Math.Max(2, Math.Min(folds, sampleCount));
        int[] foldOf = AssignStratifiedFolds(labels, folds);

        var probabilities = new double[sampleCount];
        string modelName = string.Empty;
        for (int fold = 0; fold < folds; fold++)
        {
            List<int> trainIndexes = Enumerable.Range(0, sampleCount).Where(index => foldOf[index] != fold).ToList();
            List<int> testIndexes = Enumerable.Range(0, sampleCount).Where(index => foldOf[index] == fold).ToList();
            if (testIndexes.Count == 0 || trainIndexes.Count == 0)
            {
                continue;
            }

            IClassifier model = createModel();
            modelName = model.Name;
            model.Train(trainIndexes.Select(index => features[index]).ToList(), trainIndexes.Select(index => labels[index]).ToList());
            foreach (int index in testIndexes)
            {
                probabilities[index] = model.PredictProbability(features[index]);
            }
        }

        return Score(modelName, probabilities, labels);
    }

    /// <summary>
    /// Deals the "yes" examples round-robin into folds, then the "no" examples, so every fold gets a
    /// similar share of each. Deterministic, so results are the same every run.
    /// </summary>
    private static int[] AssignStratifiedFolds(IReadOnlyList<bool> labels, int folds)
    {
        var foldOf = new int[labels.Count];
        int yesSeen = 0;
        int noSeen = 0;
        for (int index = 0; index < labels.Count; index++)
        {
            foldOf[index] = labels[index] ? yesSeen++ % folds : noSeen++ % folds;
        }

        return foldOf;
    }

    /// <summary>Accuracy, precision and recall at a 50% threshold, plus average log loss.</summary>
    private static ModelEvaluation Score(string modelName, double[] probabilities, IReadOnlyList<bool> labels)
    {
        int truePositives = 0, falsePositives = 0, trueNegatives = 0, falseNegatives = 0;
        double totalLogLoss = 0;

        for (int index = 0; index < labels.Count; index++)
        {
            bool predictedYes = probabilities[index] >= 0.5;
            bool actualYes = labels[index];
            if (predictedYes && actualYes) truePositives++;
            else if (predictedYes) falsePositives++;
            else if (actualYes) falseNegatives++;
            else trueNegatives++;

            double clipped = Math.Clamp(probabilities[index], ProbabilityFloor, 1 - ProbabilityFloor);
            totalLogLoss -= actualYes ? Math.Log(clipped) : Math.Log(1 - clipped);
        }

        return new ModelEvaluation
        {
            ModelName = modelName,
            SampleSize = labels.Count,
            Accuracy = (truePositives + trueNegatives) / (double)labels.Count,
            Precision = truePositives + falsePositives == 0 ? 0 : truePositives / (double)(truePositives + falsePositives),
            Recall = truePositives + falseNegatives == 0 ? 0 : truePositives / (double)(truePositives + falseNegatives),
            LogLoss = totalLogLoss / labels.Count
        };
    }
}
