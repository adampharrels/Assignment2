using System.ComponentModel.DataAnnotations;

namespace JobTracker.Core.Calendar;

/// <summary>Converts interview times independently of the browser or server's local time zone.</summary>
public static class SydneyTime
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");

    /// <summary>Displays a stored UTC timestamp as Sydney wall-clock time.</summary>
    public static DateTime FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    /// <summary>Converts Sydney input to UTC, rejecting missing or repeated daylight-saving times.</summary>
    public static DateTime ToUtc(DateTime local)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (Zone.IsInvalidTime(local))
            throw new ValidationException("This Sydney time does not exist because daylight saving starts. Choose another time.");
        if (Zone.IsAmbiguousTime(local))
            throw new ValidationException("This Sydney time occurs twice because daylight saving ends. Choose a time outside the repeated hour.");
        return TimeZoneInfo.ConvertTimeToUtc(local, Zone);
    }
}
