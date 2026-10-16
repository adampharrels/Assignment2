using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.Statistics;

/// <summary>Descriptive statistics over a list of applications: summary, funnel, trend and breakdowns.</summary>
public static class PipelineStatistics
{
    /// <summary>Headline counts and rates. Returns all zeros for an empty list.</summary>
    public static PipelineSummary Summarise(IReadOnlyList<ApplicationRecord> records)
    {
        int total = records.Count;
        int responses = records.Count(record => record.GotResponse());
        int interviews = records.Count(record => record.GotInterview());
        int offers = records.Count(record => record.HighestStage >= PipelineStage.Offer);
        int ghosted = records.Count(record => record.Outcome == ApplicationOutcome.Ghosted);

        List<double> responseDays = records
            .Select(record => record.DaysToFirstResponse())
            .Where(days => days.HasValue)
            .Select(days => (double)days!.Value)
            .ToList();

        return new PipelineSummary
        {
            TotalApplications = total,
            Responses = responses,
            Interviews = interviews,
            FinalRounds = records.Count(record => record.HighestStage >= PipelineStage.FinalRound),
            Offers = offers,
            Active = records.Count(record => record.Outcome == ApplicationOutcome.InProgress),
            ResponseRate = Share(responses, total),
            InterviewRate = Share(interviews, total),
            OfferRate = Share(offers, total),
            GhostingRate = Share(ghosted, total),
            MedianDaysToResponse = RateStatistics.Median(responseDays)
        };
    }

    /// <summary>Counts at each step of the hiring funnel, with conversion between steps.</summary>
    public static List<FunnelStage> BuildFunnel(IReadOnlyList<ApplicationRecord> records)
    {
        var steps = new List<(string Name, Func<ApplicationRecord, bool> Reached)>
        {
            ("Applied", record => true),
            ("Response", record => record.GotResponse()),
            ("Interview", record => record.GotInterview()),
            ("Final round", record => record.HighestStage >= PipelineStage.FinalRound),
            ("Offer", record => record.HighestStage >= PipelineStage.Offer)
        };

        var funnel = new List<FunnelStage>();
        int previousCount = records.Count;
        foreach ((string name, Func<ApplicationRecord, bool> reached) in steps)
        {
            int count = records.Count(reached);
            funnel.Add(new FunnelStage
            {
                Name = name,
                Count = count,
                ConversionFromPrevious = Share(count, previousCount),
                ConversionFromStart = Share(count, records.Count)
            });
            previousCount = count;
        }

        return funnel;
    }

    /// <summary>Applications and interviews per week, oldest first, with empty weeks filled in as zero.</summary>
    public static List<TrendPoint> WeeklyTrend(IReadOnlyList<ApplicationRecord> records)
    {
        var trend = new List<TrendPoint>();
        if (records.Count == 0)
        {
            return trend;
        }

        Dictionary<DateTime, List<ApplicationRecord>> byWeek = records
            .GroupBy(record => StartOfWeek(record.DateApplied))
            .ToDictionary(group => group.Key, group => group.ToList());

        DateTime firstWeek = byWeek.Keys.Min();
        DateTime lastWeek = byWeek.Keys.Max();
        for (DateTime week = firstWeek; week <= lastWeek; week = week.AddDays(7))
        {
            List<ApplicationRecord> weekRecords = byWeek.TryGetValue(week, out List<ApplicationRecord>? found)
                ? found
                : new List<ApplicationRecord>();
            trend.Add(new TrendPoint
            {
                WeekStart = week,
                Applications = weekRecords.Count,
                Interviews = weekRecords.Count(record => record.GotInterview())
            });
        }

        return trend;
    }

    /// <summary>
    /// Interview rates for one breakdown. Skill breakdowns need a skill extractor, so they are
    /// handled by the analytics service; this method covers the breakdowns based on plain fields.
    /// </summary>
    public static List<GroupRate> InterviewRates(IReadOnlyList<ApplicationRecord> records, RateBreakdown breakdown)
    {
        Func<ApplicationRecord, bool> gotInterview = record => record.GotInterview();

        switch (breakdown)
        {
            case RateBreakdown.Source:
                return RateStatistics.GroupRates(records, record => record.Source, gotInterview);
            case RateBreakdown.Location:
                return RateStatistics.GroupRates(records, record => record.Location, gotInterview);
            case RateBreakdown.WorkMode:
                return RateStatistics.GroupRates(records, record => record.WorkMode, gotInterview);
            case RateBreakdown.SalaryBand:
                return RateStatistics.GroupRates(records, record => record.SalaryBand(), gotInterview);
            case RateBreakdown.Tactics:
                return RateStatistics.GroupRates(records, TacticGroups, gotInterview);
            default:
                throw new ArgumentOutOfRangeException(nameof(breakdown), breakdown, "Use the analytics service for skill breakdowns.");
        }
    }

    /// <summary>The Monday on or before the given date.</summary>
    public static DateTime StartOfWeek(DateTime date)
    {
        int daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }

    /// <summary>Puts every application in one "with" or "without" group for each tactic.</summary>
    private static IEnumerable<string> TacticGroups(ApplicationRecord record)
    {
        yield return record.HadReferral ? "Referral" : "No referral";
        yield return record.TailoredResume ? "Tailored resume" : "Generic resume";
        yield return record.WroteCoverLetter ? "Cover letter" : "No cover letter";
        yield return record.AppliedEarly() ? "Applied within 3 days" : "Applied later";
    }

    private static double Share(int part, int whole)
    {
        return whole == 0 ? 0 : part / (double)whole;
    }
}
