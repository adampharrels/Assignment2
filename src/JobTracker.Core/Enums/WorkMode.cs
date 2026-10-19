namespace JobTracker.Core.Enums;

/// <summary>Where the job is done. Shown as display text by ApplicationEnumExtensions.</summary>
public enum WorkMode
{
    /// <summary>In the office every day.</summary>
    OnSite = 0,
    /// <summary>Partly in the office, partly from home.</summary>
    Hybrid = 1,
    /// <summary>Fully from home.</summary>
    Remote = 2
}
