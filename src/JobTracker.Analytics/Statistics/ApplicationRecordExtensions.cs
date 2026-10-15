using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.Statistics;

/// <summary>
/// Derived facts about an application. Analytics uses these instead of repeating the rules,
/// so "what counts as an interview" is decided in exactly one place.
/// </summary>
public static class ApplicationRecordExtensions
{
    /// <summary>Applying within this many days of the job being posted counts as "early".</summary>
    public const int EarlyApplicationDays = 3;

    /// <summary>True when the application reached at least one interview, even if later rejected.</summary>
    public static bool GotInterview(this ApplicationRecord record)
    {
        return record.HighestStage >= PipelineStage.Interview;
    }

    /// <summary>True when the company replied in any way, including a rejection.</summary>
    public static bool GotResponse(this ApplicationRecord record)
    {
        return record.DateFirstResponse.HasValue
            || record.HighestStage > PipelineStage.Applied
            || record.Outcome == ApplicationOutcome.Rejected;
    }

    /// <summary>Whole days from applying to the first reply, or null when there has been no reply.</summary>
    public static int? DaysToFirstResponse(this ApplicationRecord record)
    {
        if (!record.DateFirstResponse.HasValue)
        {
            return null;
        }

        return (int)(record.DateFirstResponse.Value.Date - record.DateApplied.Date).TotalDays;
    }

    /// <summary>True when the user applied within a few days of the job being posted.</summary>
    public static bool AppliedEarly(this ApplicationRecord record)
    {
        if (!record.DatePosted.HasValue)
        {
            return false;
        }

        return (record.DateApplied.Date - record.DatePosted.Value.Date).TotalDays <= EarlyApplicationDays;
    }

    /// <summary>Middle of the salary range, whichever bound is known, or null.</summary>
    public static decimal? SalaryMidpoint(this ApplicationRecord record)
    {
        if (record.SalaryMin.HasValue && record.SalaryMax.HasValue)
        {
            return (record.SalaryMin.Value + record.SalaryMax.Value) / 2;
        }

        if (record.SalaryMin.HasValue)
        {
            return record.SalaryMin.Value;
        }

        return record.SalaryMax;
    }

    /// <summary>The salary band label used for grouping, e.g. "$80k-$90k".</summary>
    public static string SalaryBand(this ApplicationRecord record)
    {
        decimal? salary = record.SalaryMidpoint();
        if (!salary.HasValue)
        {
            return "Not stated";
        }

        if (salary.Value < 80_000)
        {
            return "Under $80k";
        }

        if (salary.Value < 90_000)
        {
            return "$80k-$90k";
        }

        if (salary.Value < 100_000)
        {
            return "$90k-$100k";
        }

        return "$100k+";
    }
}
