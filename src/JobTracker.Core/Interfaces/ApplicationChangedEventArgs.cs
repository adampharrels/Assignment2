using JobTracker.Core.Enums;

namespace JobTracker.Core.Interfaces;

/// <summary>Sent with IApplicationService.ApplicationsChanged so other screens can refresh.</summary>
public class ApplicationChangedEventArgs : EventArgs
{
    /// <summary>Creates the event data.</summary>
    /// <param name="applicationId">The application that changed.</param>
    /// <param name="kind">What happened to it.</param>
    public ApplicationChangedEventArgs(int applicationId, ApplicationChangeKind kind)
    {
        ApplicationId = applicationId;
        Kind = kind;
    }

    /// <summary>The application that changed.</summary>
    public int ApplicationId { get; }

    /// <summary>What happened to it.</summary>
    public ApplicationChangeKind Kind { get; }
}
