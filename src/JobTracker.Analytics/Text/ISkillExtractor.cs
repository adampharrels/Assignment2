namespace JobTracker.Analytics.Text;

/// <summary>Finds the skills mentioned in a piece of text, such as a job description or a resume.</summary>
public interface ISkillExtractor
{
    /// <summary>
    /// Returns every skill mention as its canonical name, in order, including repeats
    /// (a description that says "React" twice gives "react" twice). Empty text gives an empty list.
    /// </summary>
    IReadOnlyList<string> Extract(string? text);
}
