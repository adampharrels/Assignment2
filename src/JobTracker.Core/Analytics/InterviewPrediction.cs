namespace JobTracker.Core.Analytics;

/// <summary>The predicted chance that an application leads to an interview, with the reasons.</summary>
public class InterviewPrediction
{
    /// <summary>Name of the model that made the prediction.</summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>Predicted probability of reaching an interview, between 0 and 1.</summary>
    public double Probability { get; set; }

    /// <summary>Cosine similarity between the resume and the job description, between 0 and 1.</summary>
    public double ResumeFitScore { get; set; }

    /// <summary>The inputs that moved the prediction the most, biggest first.</summary>
    public List<FeatureContribution> TopFactors { get; set; } = new List<FeatureContribution>();

    /// <summary>Skills in the job description that the user has.</summary>
    public List<string> MatchedSkills { get; set; } = new List<string>();

    /// <summary>Skills in the job description that the user does not have.</summary>
    public List<string> MissingSkills { get; set; } = new List<string>();
}
