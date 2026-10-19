using System.ComponentModel.DataAnnotations;
using System.Text;
using JobTracker.Core.Calendar;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Tests.Core.Calendar;

[TestFixture]
public class CalendarTests
{
    [TestCase(1, 11)]
    [TestCase(7, 10)]
    public void SydneyConversion_UsesSeasonalOffset_AndRoundTrips(int month, int offset)
    {
        var utc = new DateTime(2026, month, 15, 0, 0, 0, DateTimeKind.Utc);
        Assert.That(SydneyTime.FromUtc(utc), Is.EqualTo(utc.AddHours(offset)));
        Assert.That(SydneyTime.ToUtc(SydneyTime.FromUtc(utc)), Is.EqualTo(utc));
    }

    [TestCase(10, 4)]
    [TestCase(4, 5)]
    public void SydneyConversion_RejectsNonexistentOrRepeatedTime(int month, int day) =>
        Assert.Throws<ValidationException>(() => SydneyTime.ToUtc(new DateTime(2026, month, day, 2, 30, 0)));

    [Test]
    public void SydneyMonthBounds_IncludeUtcPreviousDay()
    {
        Assert.That(SydneyTime.ToUtc(new DateTime(2026, 10, 1)),
            Is.EqualTo(new DateTime(2026, 9, 30, 14, 0, 0, DateTimeKind.Utc)));
        Assert.That(SydneyTime.ToUtc(new DateTime(2026, 11, 1)),
            Is.EqualTo(new DateTime(2026, 10, 31, 13, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void Export_ContainsStableIdsUtcDurationCancellationAndEscapedFoldedText()
    {
        var start = new DateTime(2026, 10, 19, 1, 30, 0, DateTimeKind.Utc);
        var notes = string.Concat(Enumerable.Repeat("面接🙂", 40)) + ", next; line\nEND:VEVENT\n\\";
        var item = new Interview
        {
            Id = 42,
            ApplicationId = 1,
            ScheduledAt = start,
            DurationMinutes = 45,
            Outcome = InterviewOutcome.Cancelled,
            PrepNotes = notes,
            LocationOrLink = "Sydney, NSW; office"
        };
        var export = InterviewCalendarExport.Create(new[] { item }, _ => "Company · Role", start);
        var unfolded = export.Replace("\r\n ", "");
        Assert.That(unfolded, Does.Contain("UID:interview-42@grad-job-tracker.local\r\n"));
        Assert.That(unfolded, Does.Contain("DTSTART:20261019T013000Z\r\nDTEND:20261019T021500Z"));
        Assert.That(unfolded, Does.Contain("STATUS:CANCELLED"));
        Assert.That(unfolded, Does.Contain("LOCATION:Sydney\\, NSW\\; office"));
        Assert.That(unfolded, Does.Contain("\\, next\\; line\\nEND:VEVENT\\n\\\\"));
        Assert.That(export.Split("\r\n").All(line => Encoding.UTF8.GetByteCount(line) <= 75), Is.True);
        Assert.That(unfolded.Split("\r\n").Count(line => line == "END:VEVENT"), Is.EqualTo(1));
        Assert.That(export, Does.EndWith("END:VCALENDAR\r\n"));
    }

    [Test]
    public void EmptyExport_ProducesValidCalendarEnvelope()
    {
        var export = InterviewCalendarExport.Create(Array.Empty<Interview>(), _ => "", DateTime.UtcNow);
        Assert.That(export, Does.StartWith("BEGIN:VCALENDAR\r\nVERSION:2.0"));
        Assert.That(export, Does.Not.Contain("BEGIN:VEVENT"));
        Assert.That(export, Does.EndWith("END:VCALENDAR\r\n"));
    }
}
