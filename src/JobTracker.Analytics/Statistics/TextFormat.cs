using System.Globalization;

namespace JobTracker.Analytics.Statistics;

/// <summary>
/// Formats numbers in analytics text the same way on every computer. The built-in "P0" format
/// depends on the machine's culture (some print "50 %" with a space), which would make insight
/// text differ between a Mac, a lab PC and the CI server.
/// </summary>
public static class TextFormat
{
    /// <summary>0.5 becomes "50%".</summary>
    public static string Percent(double share)
    {
        return Math.Round(share * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>2.04 becomes "2.0x".</summary>
    public static string Times(double ratio)
    {
        return ratio.ToString("0.0", CultureInfo.InvariantCulture) + "x";
    }
}
