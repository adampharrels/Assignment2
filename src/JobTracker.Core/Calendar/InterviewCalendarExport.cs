using System.Globalization;
using System.Text;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Calendar;

/// <summary>Writes RFC 5545 iCalendar files with UTC times, escaped text and UTF-8 line folding.</summary>
public static class InterviewCalendarExport
{
    /// <summary>Exports interviews with stable event IDs; cancelled interviews retain their cancelled status.</summary>
    public static string Create(IEnumerable<Interview> interviews, Func<int, string> applicationLabel, DateTime generatedUtc)
    {
        var output = new StringBuilder();
        void Line(string value)
        {
            int bytes = 0;
            foreach (var rune in value.EnumerateRunes())
            {
                if (bytes + rune.Utf8SequenceLength > 75)
                {
                    output.Append("\r\n ");
                    bytes = 1;
                }
                output.Append(rune.ToString());
                bytes += rune.Utf8SequenceLength;
            }
            output.Append("\r\n");
        }
        Line("BEGIN:VCALENDAR");
        Line("VERSION:2.0");
        Line("PRODID:-//Grad Job Tracker//Interview Calendar//EN");
        Line("CALSCALE:GREGORIAN");
        foreach (var item in interviews.OrderBy(item => item.ScheduledAt).ThenBy(item => item.Id))
        {
            Line("BEGIN:VEVENT");
            Line($"UID:interview-{item.Id}@grad-job-tracker.local");
            Line("DTSTAMP:" + Timestamp(generatedUtc));
            Line("DTSTART:" + Timestamp(item.ScheduledAt));
            Line("DTEND:" + Timestamp(item.ScheduledAt.AddMinutes(item.DurationMinutes)));
            Line("SUMMARY:" + Escape($"{item.Type} interview · {applicationLabel(item.ApplicationId)}"));
            Line("LOCATION:" + Escape(item.LocationOrLink ?? string.Empty));
            Line("DESCRIPTION:" + Escape($"Outcome: {item.Outcome}\nInterviewer: {item.Interviewer}\n{item.PrepNotes}"));
            Line("STATUS:" + (item.Outcome == InterviewOutcome.Cancelled ? "CANCELLED" : "CONFIRMED"));
            Line("END:VEVENT");
        }
        Line("END:VCALENDAR");
        return output.ToString();
    }

    private static string Timestamp(DateTime utc) => utc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
    private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("\r\n", "\n")
        .Replace("\r", "\n").Replace("\n", "\\n").Replace(";", "\\;").Replace(",", "\\,");
}
