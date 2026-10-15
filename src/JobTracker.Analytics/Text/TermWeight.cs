namespace JobTracker.Analytics.Text;

/// <summary>How important a term is across a collection of documents.</summary>
public class TermWeight
{
    /// <summary>The term (word or skill name).</summary>
    public string Term { get; set; } = string.Empty;

    /// <summary>Number of documents containing the term.</summary>
    public int DocumentCount { get; set; }

    /// <summary>Average TF-IDF weight in the documents that contain the term.</summary>
    public double AverageWeight { get; set; }

    /// <summary>Sum of the term's TF-IDF weights over all documents; used for ranking.</summary>
    public double TotalWeight { get; set; }
}
