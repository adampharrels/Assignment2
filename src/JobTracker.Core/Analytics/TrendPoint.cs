namespace JobTracker.Core.Analytics;

/// <summary>Applications sent and interviews gained in one week.</summary>
public class TrendPoint
{
    /// <summary>The Monday the week starts on.</summary>
    public DateTime WeekStart { get; set; }

    /// <summary>Applications submitted that week.</summary>
    public int Applications { get; set; }

    /// <summary>Of those applications, how many reached an interview.</summary>
    public int Interviews { get; set; }
}
