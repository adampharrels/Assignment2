using JobTracker.Analytics.Statistics;
using JobTracker.Analytics.Text;
using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.MachineLearning;

/// <summary>
/// Turns an application into a row of numbers a model can learn from.
/// Yes/no facts become 0 or 1; numeric facts (resume fit, skill match, salary) are standardised
/// to "how many standard deviations above average", so no feature dominates just because of its units.
/// </summary>
public class FeatureBuilder
{
    private const int TopSkillCount = 6;

    private readonly ISkillExtractor _skillExtractor;
    private readonly TfIdfVectorizer _textVectorizer = new TfIdfVectorizer();
    private readonly List<string> _featureNames = new List<string>();
    private HashSet<string> _profileSkills = new HashSet<string>();
    private Dictionary<string, double> _resumeVector = new Dictionary<string, double>();
    private List<string> _sources = new List<string>();
    private List<string> _workModes = new List<string>();
    private List<string> _topSkills = new List<string>();
    private Scaler _fitScaler = new Scaler(0, 1);
    private Scaler _matchScaler = new Scaler(0, 1);
    private Scaler _salaryScaler = new Scaler(0, 1);

    /// <summary>Creates a builder that uses the given skill extractor.</summary>
    public FeatureBuilder(ISkillExtractor skillExtractor)
    {
        _skillExtractor = skillExtractor;
    }

    /// <summary>The name of each value in a built row, in the same order.</summary>
    public IReadOnlyList<string> FeatureNames => _featureNames;

    /// <summary>
    /// Learns everything the features depend on from the training applications: the vocabulary for
    /// resume fit, the categories to one-hot encode, the most common skills and the averages for scaling.
    /// </summary>
    public void Fit(IReadOnlyList<ApplicationRecord> records, string resumeText, IEnumerable<string> profileSkills)
    {
        _profileSkills = new HashSet<string>(profileSkills, StringComparer.OrdinalIgnoreCase);
        _textVectorizer.Fit(records.Select(record => record.JobDescription).Append(resumeText));
        _resumeVector = _textVectorizer.Transform(resumeText);

        _sources = records.Select(record => record.Source).Distinct().OrderBy(source => source).ToList();
        _workModes = records.Select(record => record.WorkMode).Distinct().OrderBy(mode => mode).ToList();
        _topSkills = records
            .SelectMany(record => _skillExtractor.Extract(record.JobDescription).Distinct())
            .GroupBy(skill => skill)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(TopSkillCount)
            .Select(group => group.Key)
            .ToList();

        _fitScaler = Scaler.FitTo(records.Select(record => ResumeFit(record.JobDescription)));
        _matchScaler = Scaler.FitTo(records.Select(record => SkillMatch(record.JobDescription)));
        _salaryScaler = Scaler.FitTo(records
            .Where(record => record.SalaryMidpoint().HasValue)
            .Select(record => (double)record.SalaryMidpoint()!.Value));

        _featureNames.Clear();
        _featureNames.AddRange(new[] { "Referral", "Tailored resume", "Cover letter", "Applied within 3 days", "Resume fit", "Skill match", "Salary" });
        _featureNames.AddRange(_sources.Select(source => "Source: " + source));
        _featureNames.AddRange(_workModes.Select(mode => "Work mode: " + mode));
        _featureNames.AddRange(_topSkills.Select(skill => "Asks for " + skill));
    }

    /// <summary>Builds the feature row for one application. Call Fit first.</summary>
    public double[] Build(ApplicationRecord record)
    {
        decimal? salary = record.SalaryMidpoint();
        HashSet<string> skills = _skillExtractor.Extract(record.JobDescription).ToHashSet();

        var row = new List<double>
        {
            record.HadReferral ? 1 : 0,
            record.TailoredResume ? 1 : 0,
            record.WroteCoverLetter ? 1 : 0,
            record.AppliedEarly() ? 1 : 0,
            _fitScaler.Scale(ResumeFit(record.JobDescription)),
            _matchScaler.Scale(SkillMatch(record.JobDescription)),
            salary.HasValue ? _salaryScaler.Scale((double)salary.Value) : 0 // Unknown salary counts as average.
        };
        row.AddRange(_sources.Select(source => record.Source == source ? 1.0 : 0.0));
        row.AddRange(_workModes.Select(mode => record.WorkMode == mode ? 1.0 : 0.0));
        row.AddRange(_topSkills.Select(skill => skills.Contains(skill) ? 1.0 : 0.0));
        return row.ToArray();
    }

    /// <summary>Cosine similarity (0-1) between the user's resume and a job description.</summary>
    public double ResumeFit(string jobDescription)
    {
        return TfIdfVectorizer.CosineSimilarity(_resumeVector, _textVectorizer.Transform(jobDescription));
    }

    /// <summary>Share (0-1) of the job's skills that the user has; 0 when the job lists no skills.</summary>
    public double SkillMatch(string jobDescription)
    {
        List<string> skills = _skillExtractor.Extract(jobDescription).Distinct().ToList();
        if (skills.Count == 0)
        {
            return 0;
        }

        return skills.Count(skill => _profileSkills.Contains(skill)) / (double)skills.Count;
    }

    /// <summary>Standardises numbers: (value - mean) / standard deviation, clamped to +/-3.</summary>
    private sealed class Scaler
    {
        private readonly double _mean;
        private readonly double _standardDeviation;

        public Scaler(double mean, double standardDeviation)
        {
            _mean = mean;
            _standardDeviation = standardDeviation > 0 ? standardDeviation : 1;
        }

        public static Scaler FitTo(IEnumerable<double> values)
        {
            List<double> list = values.ToList();
            if (list.Count == 0)
            {
                return new Scaler(0, 1);
            }

            double mean = list.Average();
            double variance = list.Sum(value => (value - mean) * (value - mean)) / list.Count;
            return new Scaler(mean, Math.Sqrt(variance));
        }

        public double Scale(double value)
        {
            return Math.Clamp((value - _mean) / _standardDeviation, -3, 3);
        }
    }
}
