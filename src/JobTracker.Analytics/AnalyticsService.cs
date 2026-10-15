using JobTracker.Analytics.Insights;
using JobTracker.Analytics.MachineLearning;
using JobTracker.Analytics.Statistics;
using JobTracker.Analytics.Text;
using JobTracker.Core.Analytics;
using JobTracker.Core.Entities;
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
    private readonly ISkillExtractor _skillExtractor;
    private readonly InsightEngine _insightEngine = new InsightEngine();

    /// <summary>Creates the service from the application data, the user's profile and a skill extractor.</summary>
    public AnalyticsService(IApplicationDataSource dataSource, IProfileService profileService, ISkillExtractor skillExtractor)
    {
        _dataSource = dataSource;
        _profileService = profileService;
        _skillExtractor = skillExtractor;
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
            SkillAnalysis skills = await CreateSkillAnalysisAsync(records, cancellationToken);
            return skills.InterviewRates();
        }

        return PipelineStatistics.InterviewRates(records, breakdown);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SkillDemand>> GetSkillDemandAsync(int top = 20, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationRecord> records = await _dataSource.GetApplicationsAsync(cancellationToken);
        SkillAnalysis skills = await CreateSkillAnalysisAsync(records, cancellationToken);
        return skills.Demand(top);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SkillGap>> GetSkillGapAsync(int top = 10, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationRecord> records = await _dataSource.GetApplicationsAsync(cancellationToken);
        SkillAnalysis skills = await CreateSkillAnalysisAsync(records, cancellationToken);
        return skills.Gap(top);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Insight>> GetInsightsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationRecord> records = await _dataSource.GetApplicationsAsync(cancellationToken);
        SkillAnalysis skills = await CreateSkillAnalysisAsync(records, cancellationToken);

        var context = new InsightContext
        {
            Records = records,
            Summary = PipelineStatistics.Summarise(records),
            SourceRates = PipelineStatistics.InterviewRates(records, RateBreakdown.Source),
            TacticRates = PipelineStatistics.InterviewRates(records, RateBreakdown.Tactics),
            SkillRates = skills.InterviewRates(),
            SkillGaps = skills.Gap(5)
        };
        return _insightEngine.Generate(context);
    }

    /// <inheritdoc />
    public async Task<InterviewPrediction?> PredictAsync(int applicationId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationRecord> records = await _dataSource.GetApplicationsAsync(cancellationToken);
        ApplicationRecord? record = records.FirstOrDefault(item => item.Id == applicationId);
        if (record == null)
        {
            return null;
        }

        InterviewPredictor predictor = await CreatePredictorAsync(records, cancellationToken);
        return predictor.Predict(record);
    }

    /// <inheritdoc />
    public async Task<InterviewPrediction> PredictWhatIfAsync(WhatIfInput input, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationRecord> records = await _dataSource.GetApplicationsAsync(cancellationToken);
        InterviewPredictor predictor = await CreatePredictorAsync(records, cancellationToken);

        // A pretend application built from the form, as if the user applied today.
        var record = new ApplicationRecord
        {
            JobDescription = input.JobDescription,
            Source = input.Source,
            WorkMode = input.WorkMode,
            SalaryMin = input.SalaryMin,
            SalaryMax = input.SalaryMax,
            HadReferral = input.HadReferral,
            TailoredResume = input.TailoredResume,
            WroteCoverLetter = input.WroteCoverLetter,
            DateApplied = DateTime.Today
        };
        return predictor.Predict(record);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelEvaluation>> EvaluateModelsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationRecord> records = await _dataSource.GetApplicationsAsync(cancellationToken);
        InterviewPredictor predictor = await CreatePredictorAsync(records, cancellationToken);
        return predictor.Evaluate();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FeatureContribution>> GetModelWeightsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationRecord> records = await _dataSource.GetApplicationsAsync(cancellationToken);
        InterviewPredictor predictor = await CreatePredictorAsync(records, cancellationToken);
        return predictor.Weights();
    }

    /// <summary>Trains the interview model on the applications, using the user's resume and skills.</summary>
    private async Task<InterviewPredictor> CreatePredictorAsync(IReadOnlyList<ApplicationRecord> records, CancellationToken cancellationToken)
    {
        UserProfile profile = await _profileService.GetProfileAsync(cancellationToken);
        IReadOnlyList<string> profileSkills = await _profileService.GetProfileSkillNamesAsync(cancellationToken);
        return new InterviewPredictor(records, profile.ResumeText, profileSkills, _skillExtractor);
    }

    /// <summary>Runs skill extraction over every application, using the user's current profile skills.</summary>
    private async Task<SkillAnalysis> CreateSkillAnalysisAsync(IReadOnlyList<ApplicationRecord> records, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> profileSkills = await _profileService.GetProfileSkillNamesAsync(cancellationToken);
        return new SkillAnalysis(records, _skillExtractor, profileSkills);
    }
}
