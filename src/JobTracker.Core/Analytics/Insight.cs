namespace JobTracker.Core.Analytics;

/// <summary>Whether an insight is good news, bad news or advice.</summary>
public enum InsightKind
{
    /// <summary>Something that is working well.</summary>
    Positive = 0,
    /// <summary>Something that is hurting results.</summary>
    Negative = 1,
    /// <summary>A neutral observation.</summary>
    Neutral = 2,
    /// <summary>A suggested action.</summary>
    Recommendation = 3
}

/// <summary>A plain-English finding, e.g. "Referral applications reached interview 2.4x as often".</summary>
public class Insight
{
    /// <summary>Short headline.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>One or two sentences with the numbers behind the headline.</summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>Good news, bad news, neutral or advice.</summary>
    public InsightKind Kind { get; set; }

    /// <summary>Used to rank insights; higher is shown first.</summary>
    public double Importance { get; set; }

    /// <summary>How many applications the insight is based on.</summary>
    public int SampleSize { get; set; }
}
