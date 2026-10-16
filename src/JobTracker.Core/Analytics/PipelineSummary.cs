namespace JobTracker.Core.Analytics;

/// <summary>Headline numbers for KPI cards on the dashboard and analytics page.</summary>
public class PipelineSummary
{
    /// <summary>Number of submitted applications.</summary>
    public int TotalApplications { get; set; }

    /// <summary>Applications that got any reply, including a rejection.</summary>
    public int Responses { get; set; }

    /// <summary>Applications that reached at least one interview.</summary>
    public int Interviews { get; set; }

    /// <summary>Applications that reached a final round.</summary>
    public int FinalRounds { get; set; }

    /// <summary>Applications that led to an offer.</summary>
    public int Offers { get; set; }

    /// <summary>Applications still in progress.</summary>
    public int Active { get; set; }

    /// <summary>Responses / TotalApplications, between 0 and 1.</summary>
    public double ResponseRate { get; set; }

    /// <summary>Interviews / TotalApplications, between 0 and 1.</summary>
    public double InterviewRate { get; set; }

    /// <summary>Offers / TotalApplications, between 0 and 1.</summary>
    public double OfferRate { get; set; }

    /// <summary>Share of applications that were ghosted, between 0 and 1.</summary>
    public double GhostingRate { get; set; }

    /// <summary>Median days from applying to the first reply, or null when nobody has replied.</summary>
    public double? MedianDaysToResponse { get; set; }
}
