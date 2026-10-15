using JobTracker.Analytics.Statistics;
using JobTracker.Core.Analytics;
using JobTracker.Core.Interfaces;

namespace JobTracker.Analytics;

/// <summary>
/// The analytics engine behind IAnalyticsService. It loads the applications, then hands them to the
/// statistics, text-mining and machine-learning classes, so each of those stays small and testable.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IApplicationDataSource _dataSource;
    private readonly IProfileService _profileService;

    /// <summary>Creates the service from the application data and the user's profile.</summary>
    public AnalyticsService(IApplicationDataSource dataSource, IProfileService profileService)
    {
        _dataSource = dataSource;
        _profileService = profileService;
    }

    /// <inheritdoc />
    public async Task<PipelineSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationRecord> records = await _dataSource.GetApplicationsAsync(cancellationToken);
        return PipelineStatistics.Summarise(records);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FunnelStage>> GetFunnelAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationRecord> records = await _dataSource.GetApplicationsAsync(cancellationToken);
        return records.Count == 0 ? new List<FunnelStage>() : PipelineStatistics.BuildFunnel(records);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrendPoint>> GetWeeklyTrendAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationRecord> records = await _dataSource.GetApplicationsAsync(cancellationToken);
        return PipelineStatistics.WeeklyTrend(records);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GroupRate>> GetInterviewRatesAsync(RateBreakdown breakdown, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationRecord> records = await _dataSource.GetApplicationsAsync(cancellationToken);
        if (breakdown == RateBreakdown.Skill)
        {
            return new List<GroupRate>(); // Needs skill extraction (A3).
        }

        return PipelineStatistics.InterviewRates(records, breakdown);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SkillDemand>> GetSkillDemandAsync(int top = 20, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<SkillDemand>>(new List<SkillDemand>()); // A3
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SkillGap>> GetSkillGapAsync(int top = 10, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<SkillGap>>(new List<SkillGap>()); // A3
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Insight>> GetInsightsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Insight>>(new List<Insight>()); // A5
    }

    /// <inheritdoc />
    public Task<InterviewPrediction?> PredictAsync(int applicationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<InterviewPrediction?>(null); // A4
    }

    /// <inheritdoc />
    public Task<InterviewPrediction> PredictWhatIfAsync(WhatIfInput input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InterviewPrediction()); // A4
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ModelEvaluation>> EvaluateModelsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ModelEvaluation>>(new List<ModelEvaluation>()); // A4
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FeatureContribution>> GetModelWeightsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FeatureContribution>>(new List<FeatureContribution>()); // A4
    }
}
