namespace JobTracker.Core.Enums;

/// <summary>What happened to an application, sent with IApplicationService.ApplicationsChanged.</summary>
public enum ApplicationChangeKind
{
    /// <summary>A new application was added.</summary>
    Created = 0,
    /// <summary>An application's details were edited.</summary>
    Updated = 1,
    /// <summary>An application moved to a new status.</summary>
    StatusChanged = 2,
    /// <summary>An application was deleted.</summary>
    Deleted = 3
}
