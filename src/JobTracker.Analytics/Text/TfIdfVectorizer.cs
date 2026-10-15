namespace JobTracker.Analytics.Text;

/// <summary>
/// TF-IDF (term frequency x inverse document frequency), written from scratch.
/// A term scores highly in a document when it appears often there (TF) but rarely in other
/// documents (IDF). Words every job ad uses ("team", "graduate") therefore get low weights,
/// and distinctive skills ("kubernetes") get high ones.
/// </summary>
/// <remarks>
/// Formulas (the same smoothing scikit-learn uses):
///   tf(t, d)  = count of t in d / number of tokens in d
///   idf(t)    = ln((1 + N) / (1 + df(t))) + 1, where N = number of documents and df(t) = documents containing t
///   weight    = tf x idf, then each document vector is scaled to length 1 (L2 normalisation)
/// </remarks>
public class TfIdfVectorizer
{
    private readonly Dictionary<string, int> _documentFrequency = new Dictionary<string, int>();

    /// <summary>Number of documents the vectorizer was fitted on.</summary>
    public int DocumentCount { get; private set; }

    /// <summary>Every term seen during fitting.</summary>
    public IReadOnlyCollection<string> Vocabulary => _documentFrequency.Keys;

    /// <summary>Learns document frequencies from already-tokenised documents. Replaces any earlier fit.</summary>
    public void Fit(IEnumerable<IReadOnlyList<string>> documents)
    {
        _documentFrequency.Clear();
        DocumentCount = 0;

        foreach (IReadOnlyList<string> document in documents)
        {
            DocumentCount++;
            foreach (string term in document.Distinct())
            {
                _documentFrequency[term] = _documentFrequency.TryGetValue(term, out int count) ? count + 1 : 1;
            }
        }
    }

    /// <summary>Learns document frequencies from raw text, tokenising it first.</summary>
    public void Fit(IEnumerable<string> texts)
    {
        Fit(texts.Select(text => (IReadOnlyList<string>)Tokenizer.Tokenize(text).ToList()));
    }

    /// <summary>The smoothed inverse document frequency of a term (unknown terms get the highest value).</summary>
    public double InverseDocumentFrequency(string term)
    {
        int documentFrequency = _documentFrequency.TryGetValue(term, out int count) ? count : 0;
        return Math.Log((1.0 + DocumentCount) / (1.0 + documentFrequency)) + 1.0;
    }

    /// <summary>
    /// Turns a tokenised document into a sparse TF-IDF vector (term to weight) of length 1.
    /// Terms not seen during fitting are ignored.
    /// </summary>
    public Dictionary<string, double> Transform(IReadOnlyList<string> tokens)
    {
        var vector = new Dictionary<string, double>();
        List<string> knownTokens = tokens.Where(token => _documentFrequency.ContainsKey(token)).ToList();
        if (knownTokens.Count == 0)
        {
            return vector;
        }

        foreach (IGrouping<string, string> group in knownTokens.GroupBy(token => token))
        {
            double termFrequency = group.Count() / (double)knownTokens.Count;
            vector[group.Key] = termFrequency * InverseDocumentFrequency(group.Key);
        }

        Normalise(vector);
        return vector;
    }

    /// <summary>Turns raw text into a TF-IDF vector, tokenising it first.</summary>
    public Dictionary<string, double> Transform(string text)
    {
        return Transform(Tokenizer.Tokenize(text).ToList());
    }

    /// <summary>
    /// Ranks terms across a set of documents by their total TF-IDF weight, so terms that are
    /// both common and distinctive come first.
    /// </summary>
    public List<TermWeight> RankTerms(IEnumerable<IReadOnlyList<string>> documents, int top)
    {
        var totals = new Dictionary<string, TermWeight>();
        foreach (IReadOnlyList<string> document in documents)
        {
            foreach (KeyValuePair<string, double> entry in Transform(document))
            {
                if (!totals.TryGetValue(entry.Key, out TermWeight? weight))
                {
                    weight = new TermWeight { Term = entry.Key };
                    totals[entry.Key] = weight;
                }

                weight.DocumentCount++;
                weight.TotalWeight += entry.Value;
            }
        }

        foreach (TermWeight weight in totals.Values)
        {
            weight.AverageWeight = weight.TotalWeight / weight.DocumentCount;
        }

        return totals.Values
            .OrderByDescending(weight => weight.TotalWeight)
            .ThenBy(weight => weight.Term)
            .Take(top)
            .ToList();
    }

    /// <summary>
    /// Cosine similarity of two vectors: 1 means they point the same way (same words in the same
    /// proportions), 0 means no words in common.
    /// </summary>
    public static double CosineSimilarity(IReadOnlyDictionary<string, double> first, IReadOnlyDictionary<string, double> second)
    {
        double dotProduct = 0;
        foreach (KeyValuePair<string, double> entry in first)
        {
            if (second.TryGetValue(entry.Key, out double otherWeight))
            {
                dotProduct += entry.Value * otherWeight;
            }
        }

        double firstLength = Math.Sqrt(first.Values.Sum(weight => weight * weight));
        double secondLength = Math.Sqrt(second.Values.Sum(weight => weight * weight));
        if (firstLength == 0 || secondLength == 0)
        {
            return 0;
        }

        return dotProduct / (firstLength * secondLength);
    }

    /// <summary>Scales the vector so its length is 1.</summary>
    private static void Normalise(Dictionary<string, double> vector)
    {
        double length = Math.Sqrt(vector.Values.Sum(weight => weight * weight));
        if (length == 0)
        {
            return;
        }

        foreach (string term in vector.Keys.ToList())
        {
            vector[term] /= length;
        }
    }
}
