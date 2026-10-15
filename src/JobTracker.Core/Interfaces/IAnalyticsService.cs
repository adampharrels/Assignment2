using JobTracker.Core.Analytics;

namespace JobTracker.Core.Interfaces;

/// <summary>
/// Statistics, text mining and machine-learning results for the dashboard, analytics page and
/// application details. Pages depend only on this interface, never on the engine behind it.
/// </summary>
/// <remarks>
/// Every method returns empty results (never throws) when there is no data, so pages can show an empty state.
/// </remarks>
public interface IAnalyticsService
{
    /// <summary>Headline counts and rates.</summary>
    Task<PipelineSummary> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>Applied, Response, Interview, Final round and Offer counts with conversion rates.</summary>
    Task<IReadOnlyList<FunnelStage>> GetFunnelAsync(CancellationToken cancellationToken = default);

    /// <summary>Applications and interviews per week, oldest first.</summary>
    Task<IReadOnlyList<TrendPoint>> GetWeeklyTrendAsync(CancellationToken cancellationToken = default);

    /// <summary>Interview rate per group with 95% confidence intervals, best first.</summary>
    Task<IReadOnlyList<GroupRate>> GetInterviewRatesAsync(RateBreakdown breakdown, CancellationToken cancellationToken = default);

    /// <summary>The most requested skills across all job descriptions.</summary>
    Task<IReadOnlyList<SkillDemand>> GetSkillDemandAsync(int top = 20, CancellationToken cancellationToken = default);

    /// <summary>In-demand skills missing from the user's profile, most valuable first.</summary>
    Task<IReadOnlyList<SkillGap>> GetSkillGapAsync(int top = 10, CancellationToken cancellationToken = default);

    /// <summary>Plain-English findings, most important first.</summary>
    Task<IReadOnlyList<Insight>> GetInsightsAsync(CancellationToken cancellationToken = default);

    /// <summary>Predicted interview chance for a stored application, or null if it does not exist.</summary>
    Task<InterviewPrediction?> PredictAsync(int applicationId, CancellationToken cancellationToken = default);

    /// <summary>Predicted interview chance for a job the user has not applied to yet.</summary>
    Task<InterviewPrediction> PredictWhatIfAsync(WhatIfInput input, CancellationToken cancellationToken = default);

    /// <summary>Cross-validated quality of each model, compared with a simple baseline.</summary>
    Task<IReadOnlyList<ModelEvaluation>> EvaluateModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>What drives interviews: the logistic-regression weight of every input, strongest first.</summary>
    Task<IReadOnlyList<FeatureContribution>> GetModelWeightsAsync(CancellationToken cancellationToken = default);
}
