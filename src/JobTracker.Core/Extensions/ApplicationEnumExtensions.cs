using JobTracker.Core.Enums;

namespace JobTracker.Core.Extensions;

/// <summary>Display text for the application enums, and parsing it back (for forms and CSV import).</summary>
public static class ApplicationEnumExtensions
{
    /// <summary>The status as shown in the UI, e.g. "Final round".</summary>
    public static string ToDisplayText(this ApplicationStatus status) => status switch
    {
        ApplicationStatus.FinalRound => "Final round",
        _ => status.ToString()
    };

    /// <summary>The source as shown in the UI and passed to analytics, e.g. "Career fair".</summary>
    public static string ToDisplayText(this ApplicationSource source) => source switch
    {
        ApplicationSource.CareerFair => "Career fair",
        ApplicationSource.CompanyWebsite => "Company website",
        _ => source.ToString()
    };

    /// <summary>The work mode as shown in the UI and passed to analytics, e.g. "On-site".</summary>
    public static string ToDisplayText(this WorkMode workMode) => workMode switch
    {
        WorkMode.OnSite => "On-site",
        _ => workMode.ToString()
    };

    /// <summary>Reads a source from display text or the enum name, ignoring case and spaces.</summary>
    /// <param name="text">For example "Career fair", "careerfair" or "LinkedIn".</param>
    /// <param name="source">The matching source, or Other when there is no match.</param>
    /// <returns>True when the text matched a source.</returns>
    public static bool TryParseSource(string? text, out ApplicationSource source) =>
        TryParseDisplayText(text, ToDisplayText, out source);

    /// <summary>Reads a work mode from display text or the enum name, ignoring case, spaces and hyphens.</summary>
    /// <param name="text">For example "On-site", "onsite" or "Remote".</param>
    /// <param name="workMode">The matching work mode, or OnSite when there is no match.</param>
    /// <returns>True when the text matched a work mode.</returns>
    public static bool TryParseWorkMode(string? text, out WorkMode workMode) =>
        TryParseDisplayText(text, ToDisplayText, out workMode);

    /// <summary>Reads a status from display text or the enum name, ignoring case and spaces.</summary>
    /// <param name="text">For example "Final round" or "Applied".</param>
    /// <param name="status">The matching status, or Wishlist when there is no match.</param>
    /// <returns>True when the text matched a status.</returns>
    public static bool TryParseStatus(string? text, out ApplicationStatus status) =>
        TryParseDisplayText(text, ToDisplayText, out status);

    /// <summary>Matches text against each enum value's name and display text, after normalising both.</summary>
    private static bool TryParseDisplayText<TEnum>(string? text, Func<TEnum, string> toDisplayText, out TEnum value)
        where TEnum : struct, Enum
    {
        string wanted = Normalise(text);
        foreach (TEnum candidate in Enum.GetValues<TEnum>())
        {
            if (wanted.Length > 0
                && (Normalise(candidate.ToString()) == wanted || Normalise(toDisplayText(candidate)) == wanted))
            {
                value = candidate;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>Lower case with spaces, hyphens and underscores removed.</summary>
    private static string Normalise(string? text) =>
        new string((text ?? string.Empty).Where(character => character is not (' ' or '-' or '_')).ToArray())
            .ToLowerInvariant();
}
