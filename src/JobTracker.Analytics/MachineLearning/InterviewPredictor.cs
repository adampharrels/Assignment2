using JobTracker.Analytics.Statistics;
using JobTracker.Analytics.Text;
using JobTracker.Core.Analytics;
using JobTracker.Core.Enums;

namespace JobTracker.Analytics.MachineLearning;

/// <summary>
/// Trains a logistic-regression model on the user's past applications and uses it to predict, and
/// explain, the interview chance of any application. Also compares all models with cross-validation.
/// </summary>
public class InterviewPredictor
{
    /// <summary>Fewer decided applications than this and the model is not trained at all.</summary>
    public const int MinimumTrainingSize = 10;

    /// <summary>An application with no reply after this many days counts as "no interview" for training.</summary>
    public const int DaysUntilDecided = 21;

    private readonly ISkillExtractor _skillExtractor;
    private readonly HashSet<string> _profileSkills;
    private readonly FeatureBuilder _featureBuilder;
    private readonly LogisticRegressionClassifier _model = new LogisticRegressionClassifier();
    private readonly List<double[]> _trainingFeatures;
    private readonly List<bool> _trainingLabels;

    /// <summary>Builds features for every application and trains the model on the decided ones.</summary>
    public InterviewPredictor(IReadOnlyList<ApplicationRecord> records, string resumeText,
        IEnumerable<string> profileSkills, ISkillExtractor skillExtractor)
    {
        _skillExtractor = skillExtractor;
        _profileSkills = new HashSet<string>(profileSkills, StringComparer.OrdinalIgnoreCase);
        _featureBuilder = new FeatureBuilder(skillExtractor);
        _featureBuilder.Fit(records, resumeText, _profileSkills);

        // "Today" is the latest application date, not the computer clock, so results are repeatable.
        DateTime latestApplication = records.Count == 0 ? DateTime.MinValue : records.Max(record => record.DateApplied);
        List<ApplicationRecord> decided = records.Where(record => IsDecided(record, latestApplication)).ToList();
        _trainingFeatures = decided.Select(record => _featureBuilder.Build(record)).ToList();
        _trainingLabels = decided.Select(record => record.GotInterview()).ToList();

        BaseRate = _trainingLabels.Count == 0 ? 0 : _trainingLabels.Count(label => label) / (double)_trainingLabels.Count;
        if (_trainingLabels.Count >= MinimumTrainingSize && _trainingLabels.Contains(true) && _trainingLabels.Contains(false))
        {
            _model.Train(_trainingFeatures, _trainingLabels);
            IsTrained = true;
        }
    }

    /// <summary>True when there was enough data (with both outcomes) to train the model.</summary>
    public bool IsTrained { get; }

    /// <summary>Interview rate of the training applications; used when the model is not trained.</summary>
    public double BaseRate { get; }

    /// <summary>
    /// True when we know whether the application got an interview: it did, it ended, or it has
    /// waited long enough that no reply means no.
    /// </summary>
    public static bool IsDecided(ApplicationRecord record, DateTime today)
    {
        return record.GotInterview()
            || record.Outcome != ApplicationOutcome.InProgress
            || (today - record.DateApplied).TotalDays >= DaysUntilDecided;
    }

    /// <summary>Predicts the interview chance of an application and explains the top 3 reasons.</summary>
    public InterviewPrediction Predict(ApplicationRecord record)
    {
        List<string> jobSkills = _skillExtractor.Extract(record.JobDescription).Distinct().ToList();
        var prediction = new InterviewPrediction
        {
            ModelName = IsTrained ? _model.Name : "Not enough data yet (showing your average)",
            ResumeFitScore = _featureBuilder.ResumeFit(record.JobDescription),
            MatchedSkills = jobSkills.Where(skill => _profileSkills.Contains(skill)).ToList(),
            MissingSkills = jobSkills
                .Where(skill => !_profileSkills.Contains(skill))
                .Where(skill => SkillDictionary.CategoryOf(skill) != SkillCategory.Soft)
                .ToList()
        };

        if (!IsTrained)
        {
            prediction.Probability = BaseRate;
            return prediction;
        }

        double[] row = _featureBuilder.Build(record);
        double[] contributions = _model.Contributions(row);
        prediction.Probability = _model.PredictProbability(row);
        prediction.TopFactors = Enumerable.Range(0, row.Length)
            .Where(index => Math.Abs(contributions[index]) > 1e-6)
            .OrderByDescending(index => Math.Abs(contributions[index]))
            .Take(3)
            .Select(index => new FeatureContribution
            {
                Feature = _featureBuilder.FeatureNames[index],
                Value = row[index],
                Contribution = contributions[index]
            })
            .ToList();
        return prediction;
    }

    /// <summary>Cross-validated comparison of logistic regression, naive Bayes and the baseline.</summary>
    public List<ModelEvaluation> Evaluate()
    {
        if (!IsTrained)
        {
            return new List<ModelEvaluation>();
        }

        var modelFactories = new List<Func<IClassifier>>
        {
            () => new LogisticRegressionClassifier(),
            () => new NaiveBayesClassifier(),
            () => new MajorityBaselineClassifier()
        };

        return modelFactories
            .Select(createModel => CrossValidator.Evaluate(createModel, _trainingFeatures, _trainingLabels))
            .ToList();
    }

    /// <summary>The trained model's weight for every feature, strongest effect first.</summary>
    public List<FeatureContribution> Weights()
    {
        if (!IsTrained)
        {
            return new List<FeatureContribution>();
        }

        return _featureBuilder.FeatureNames
            .Select((name, index) => new FeatureContribution { Feature = name, Value = 1, Contribution = _model.Weights[index] })
            .OrderByDescending(weight => Math.Abs(weight.Contribution))
            .ToList();
    }
}
