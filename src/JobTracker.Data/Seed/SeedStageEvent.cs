namespace JobTracker.Data.Seed;

/// <summary>One status change in a seed application's history, like the spec's StageEvent.</summary>
public class SeedStageEvent
{
    /// <summary>The status before the change, or null for the event that created the application.</summary>
    public SeedStatus? FromStatus { get; init; }

    /// <summary>The status after the change.</summary>
    public SeedStatus ToStatus { get; init; }

    /// <summary>The date of the change.</summary>
    public DateTime ChangedAt { get; init; }
}
