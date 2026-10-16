namespace JobTracker.Core.Analytics;

/// <summary>How much one input pushed a prediction up (positive) or down (negative).</summary>
public class FeatureContribution
{
    /// <summary>Display name of the feature, e.g. "Referral".</summary>
    public string Feature { get; set; } = string.Empty;

    /// <summary>The feature's value for this application (0/1 for yes/no features).</summary>
    public double Value { get; set; }

    /// <summary>Effect on the prediction's log-odds: model weight x value.</summary>
    public double Contribution { get; set; }
}
