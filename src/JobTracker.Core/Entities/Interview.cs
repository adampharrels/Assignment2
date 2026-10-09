using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Entities;

/// <summary>An interview linked to a job application.</summary>
public class Interview
{
    /// <summary>The database-generated identifier.</summary>
    public int Id { get; set; }

    /// <summary>The related application's identifier; the service verifies that it exists.</summary>
    [Range(1, int.MaxValue)]
    public int ApplicationId { get; set; }

    /// <summary>The kind of interview.</summary>
    [EnumDataType(typeof(InterviewType))]
    public InterviewType Type { get; set; } = InterviewType.Other;

    /// <summary>The start time in UTC; the UI converts to and from the user's local time.</summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>The planned duration, between 5 and 600 minutes inclusive.</summary>
    [Range(5, 600)]
    public int DurationMinutes { get; set; } = 60;

    /// <summary>A physical location or meeting link.</summary>
    public string? LocationOrLink { get; set; }

    /// <summary>The interviewer's name, if known.</summary>
    public string? Interviewer { get; set; }

    /// <summary>Notes to help prepare for the interview.</summary>
    public string? PrepNotes { get; set; }

    /// <summary>The result, initially Pending.</summary>
    [EnumDataType(typeof(InterviewOutcome))]
    public InterviewOutcome Outcome { get; set; } = InterviewOutcome.Pending;
}
