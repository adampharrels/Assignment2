namespace JobTracker.Core.Analytics;

/// <summary>One bar of the conversion funnel (Applied, Response, Interview, Final round, Offer).</summary>
public class FunnelStage
{
    /// <summary>Display name of the stage.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Applications that reached this stage.</summary>
    public int Count { get; set; }

    /// <summary>Count / previous stage's count, between 0 and 1.</summary>
    public double ConversionFromPrevious { get; set; }

    /// <summary>Count / number of applications, between 0 and 1.</summary>
    public double ConversionFromStart { get; set; }
}
