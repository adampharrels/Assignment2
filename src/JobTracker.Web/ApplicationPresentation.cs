using JobTracker.Core.Enums;
using MudBlazor;

namespace JobTracker.Web;

/// <summary>Shared status colours for application cards, tables and timelines.</summary>
public static class ApplicationPresentation
{
    /// <summary>Green for good news, red for endings, blue for interviews, grey for the rest.</summary>
    public static Color StatusColor(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Offer or ApplicationStatus.Accepted => Color.Success,
        ApplicationStatus.Interview or ApplicationStatus.FinalRound => Color.Info,
        ApplicationStatus.Rejected or ApplicationStatus.Ghosted => Color.Error,
        ApplicationStatus.Withdrawn => Color.Warning,
        _ => Color.Default
    };
}
