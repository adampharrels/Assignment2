using JobTracker.Core.Enums;

namespace JobTracker.Core.Extensions;

/// <summary>Readable labels for interview enums, so every screen shows "Final round" rather than "FinalRound".</summary>
public static class InterviewEnumExtensions
{
    /// <summary>The label shown to the user, e.g. "Assessment centre".</summary>
    public static string ToDisplayText(this InterviewType type) => type switch
    {
        InterviewType.AssessmentCentre => "Assessment centre",
        InterviewType.FinalRound => "Final round",
        _ => type.ToString()
    };
}
