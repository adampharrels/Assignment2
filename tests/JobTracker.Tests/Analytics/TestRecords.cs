using JobTracker.Core.Analytics;

namespace JobTracker.Tests.Analytics;

/// <summary>Small helpers for building hand-made applications in tests.</summary>
public static class TestRecords
{
    /// <summary>Builds an application with only the fields a test cares about.</summary>
    public static ApplicationRecord Make(
        PipelineStage stage = PipelineStage.Applied,
        ApplicationOutcome outcome = ApplicationOutcome.InProgress,
        string source = "LinkedIn",
        string description = "",
        bool referral = false,
        DateTime? applied = null,
        int? daysToResponse = null,
        decimal? salaryMin = null,
        decimal? salaryMax = null)
    {
        DateTime appliedDate = applied ?? new DateTime(2026, 8, 3);
        return new ApplicationRecord
        {
            HighestStage = stage,
            Outcome = outcome,
            Source = source,
            JobDescription = description,
            HadReferral = referral,
            DateApplied = appliedDate,
            DateFirstResponse = daysToResponse.HasValue ? appliedDate.AddDays(daysToResponse.Value) : null,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax
        };
    }
}
