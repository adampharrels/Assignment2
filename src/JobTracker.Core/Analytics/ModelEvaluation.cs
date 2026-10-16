namespace JobTracker.Core.Analytics;

/// <summary>Cross-validated quality of one model, so the user knows how far to trust predictions.</summary>
public class ModelEvaluation
{
    /// <summary>Name of the model.</summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>Number of applications used.</summary>
    public int SampleSize { get; set; }

    /// <summary>Share of correct yes/no predictions.</summary>
    public double Accuracy { get; set; }

    /// <summary>Of the applications predicted to get an interview, the share that did.</summary>
    public double Precision { get; set; }

    /// <summary>Of the applications that got an interview, the share the model caught.</summary>
    public double Recall { get; set; }

    /// <summary>Average log loss of the probabilities (lower is better).</summary>
    public double LogLoss { get; set; }
}
