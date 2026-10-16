using JobTracker.Analytics.Text;

namespace JobTracker.Tests.A2;

[TestFixture]
public class TfIdfVectorizerTests
{
    private static readonly string[] Corpus =
    {
        "graduate engineer react typescript",
        "graduate engineer java kubernetes",
        "graduate analyst sql python"
    };

    [Test]
    public void Transform_RareTermOutweighsTermInEveryDocument()
    {
        var vectorizer = new TfIdfVectorizer();
        vectorizer.Fit(Corpus);

        Dictionary<string, double> vector = vectorizer.Transform(Corpus[1]);

        Assert.That(vector["kubernetes"], Is.GreaterThan(vector["engineer"]));
        Assert.That(vector["engineer"], Is.GreaterThan(vector["graduate"]));
    }

    [Test]
    public void Transform_VectorHasLengthOne()
    {
        var vectorizer = new TfIdfVectorizer();
        vectorizer.Fit(Corpus);

        Dictionary<string, double> vector = vectorizer.Transform(Corpus[0]);

        double length = Math.Sqrt(vector.Values.Sum(weight => weight * weight));
        Assert.That(length, Is.EqualTo(1.0).Within(1e-9));
    }

    [Test]
    public void InverseDocumentFrequency_MatchesFormula()
    {
        var vectorizer = new TfIdfVectorizer();
        vectorizer.Fit(Corpus);

        // "graduate" is in all 3 documents: ln(4 / 4) + 1 = 1. "sql" is in 1: ln(4 / 2) + 1.
        Assert.That(vectorizer.InverseDocumentFrequency("graduate"), Is.EqualTo(1.0).Within(1e-9));
        Assert.That(vectorizer.InverseDocumentFrequency("sql"), Is.EqualTo(Math.Log(2) + 1).Within(1e-9));
    }

    [Test]
    public void Transform_UnknownWordsAreIgnored()
    {
        var vectorizer = new TfIdfVectorizer();
        vectorizer.Fit(Corpus);

        Assert.That(vectorizer.Transform("completely unseen words"), Is.Empty);
    }

    [Test]
    public void CosineSimilarity_SameIsOneAndDisjointIsZero()
    {
        var vectorizer = new TfIdfVectorizer();
        vectorizer.Fit(Corpus);
        Dictionary<string, double> react = vectorizer.Transform("react typescript");
        Dictionary<string, double> sql = vectorizer.Transform("sql python");

        Assert.That(TfIdfVectorizer.CosineSimilarity(react, react), Is.EqualTo(1.0).Within(1e-9));
        Assert.That(TfIdfVectorizer.CosineSimilarity(react, sql), Is.EqualTo(0.0));
        Assert.That(TfIdfVectorizer.CosineSimilarity(react, new Dictionary<string, double>()), Is.EqualTo(0.0));
    }

    [Test]
    public void RankTerms_PutsCommonDistinctiveTermsFirst()
    {
        var documents = new List<IReadOnlyList<string>>
        {
            new[] { "react", "sql" },
            new[] { "react", "java" },
            new[] { "react", "python" },
            new[] { "go" }
        };
        var vectorizer = new TfIdfVectorizer();
        vectorizer.Fit(documents);

        List<TermWeight> ranking = vectorizer.RankTerms(documents, top: 2);

        Assert.That(ranking[0].Term, Is.EqualTo("react"));
        Assert.That(ranking[0].DocumentCount, Is.EqualTo(3));
    }
}
