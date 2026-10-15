using JobTracker.Analytics.Statistics;
using JobTracker.Core.Analytics;
using JobTracker.Core.Enums;

namespace JobTracker.Analytics.Text;

/// <summary>
/// Skill-level analysis across all applications: which skills employers want (demand), which skills
/// go with more interviews (rates) and which missing skills the user should learn next (gap).
/// </summary>
public class SkillAnalysis
{
    private readonly IReadOnlyList<ApplicationRecord> _records;
    private readonly List<IReadOnlyList<string>> _skillsPerRecord;
    private readonly HashSet<string> _profileSkills;
    private readonly TfIdfVectorizer _vectorizer = new TfIdfVectorizer();

    /// <summary>Extracts the skills of every application once, so all the analyses share them.</summary>
    public SkillAnalysis(IReadOnlyList<ApplicationRecord> records, ISkillExtractor extractor, IEnumerable<string> profileSkills)
    {
        _records = records;
        _skillsPerRecord = records.Select(record => extractor.Extract(record.JobDescription)).ToList();
        _profileSkills = new HashSet<string>(profileSkills, StringComparer.OrdinalIgnoreCase);

        // Each job description becomes a "document" made of its skill mentions, so TF-IDF weighs
        // skills against each other rather than against filler words.
        _vectorizer.Fit(_skillsPerRecord);
    }

    /// <summary>The distinct skills found in one application's description.</summary>
    public IReadOnlyList<string> SkillsOf(int recordIndex)
    {
        return _skillsPerRecord[recordIndex].Distinct().ToList();
    }

    /// <summary>The most requested skills, ranked by total TF-IDF weight across all descriptions.</summary>
    public List<SkillDemand> Demand(int top)
    {
        if (_records.Count == 0)
        {
            return new List<SkillDemand>();
        }

        return _vectorizer.RankTerms(_skillsPerRecord, top)
            .Select(term => new SkillDemand
            {
                Skill = term.Term,
                DocumentCount = term.DocumentCount,
                DocumentShare = term.DocumentCount / (double)_records.Count,
                AverageWeight = term.AverageWeight,
                InMyProfile = _profileSkills.Contains(term.Term)
            })
            .ToList();
    }

    /// <summary>Interview rate of the applications that ask for each skill.</summary>
    public List<GroupRate> InterviewRates()
    {
        List<int> indexes = Enumerable.Range(0, _records.Count).ToList();
        return RateStatistics.GroupRates(indexes, index => SkillsOf(index), index => _records[index].GotInterview());
    }

    /// <summary>
    /// Missing skills worth learning. A skill ranks high when many jobs ask for it AND the user does
    /// worse than usual on those jobs: Score = share of jobs asking for it x (2 - lift).
    /// Lift only counts when the group is big enough to be reliable; otherwise it is treated as 1.
    /// Soft skills are left out: "teamwork" is not something to go and study.
    /// </summary>
    public List<SkillGap> Gap(int top)
    {
        if (_records.Count == 0)
        {
            return new List<SkillGap>();
        }

        Dictionary<string, GroupRate> rates = InterviewRates().ToDictionary(rate => rate.Group);
        double overallRate = _records.Count(record => record.GotInterview()) / (double)_records.Count;

        return Demand(int.MaxValue)
            .Where(demand => !demand.InMyProfile)
            .Where(demand => SkillDictionary.CategoryOf(demand.Skill) != SkillCategory.Soft)
            .Select(demand =>
            {
                GroupRate rate = rates[demand.Skill];
                double lift = rate.IsReliable ? rate.Lift : 1.0;
                return new SkillGap
                {
                    Skill = demand.Skill,
                    DocumentShare = demand.DocumentShare,
                    InterviewLift = lift,
                    Score = demand.DocumentShare * (2 - Math.Min(lift, 2)),
                    Reason = DescribeGap(demand, rate, overallRate)
                };
            })
            .OrderByDescending(gap => gap.Score)
            .ThenBy(gap => gap.Skill)
            .Take(top)
            .ToList();
    }

    /// <summary>Skills the job asks for, split into ones the user has and ones they are missing.</summary>
    public (List<string> Matched, List<string> Missing) CompareWithProfile(IEnumerable<string> jobSkills)
    {
        List<string> distinct = jobSkills.Distinct().ToList();
        List<string> matched = distinct.Where(skill => _profileSkills.Contains(skill)).ToList();
        List<string> missing = distinct.Where(skill => !_profileSkills.Contains(skill)).ToList();
        return (matched, missing);
    }

    private static string DescribeGap(SkillDemand demand, GroupRate rate, double overallRate)
    {
        string share = $"Asked for in {demand.DocumentShare:P0} of your jobs ({demand.DocumentCount}).";
        if (!rate.IsReliable)
        {
            return share + " Too few applications to judge its effect on interviews yet.";
        }

        return share + $" You reached interview in {rate.Rate:P0} of those, vs {overallRate:P0} overall.";
    }
}
