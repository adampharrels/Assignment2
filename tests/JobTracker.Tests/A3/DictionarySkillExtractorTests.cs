using JobTracker.Analytics.Text;
using JobTracker.Core.Enums;

namespace JobTracker.Tests.A3;

[TestFixture]
public class DictionarySkillExtractorTests
{
    private readonly DictionarySkillExtractor _extractor = new DictionarySkillExtractor();

    [Test]
    public void Extract_MapsSynonymsToCanonicalNames()
    {
        IReadOnlyList<string> skills = _extractor.Extract("Strong JS, k8s and Postgres. ReactJS a plus.");

        Assert.That(skills, Is.EqualTo(new[] { "javascript", "kubernetes", "postgresql", "react" }));
    }

    [Test]
    public void Extract_PrefersLongestPhrase()
    {
        IReadOnlyList<string> skills = _extractor.Extract("SQL Server, machine learning and Amazon Web Services");

        Assert.That(skills, Is.EqualTo(new[] { "sql server", "machine learning", "aws" }));
    }

    [Test]
    public void Extract_KeepsRepeatsForWeighting()
    {
        IReadOnlyList<string> skills = _extractor.Extract("Python, Python and more Python");

        Assert.That(skills.Count(skill => skill == "python"), Is.EqualTo(3));
    }

    [Test]
    public void Extract_IgnoresOrdinaryEnglish()
    {
        IReadOnlyList<string> skills = _extractor.Extract("Spring internship: the rest of the team will express interest");

        Assert.That(skills, Is.Empty);
    }

    [Test]
    public void Extract_EmptyText_ReturnsEmpty()
    {
        Assert.That(_extractor.Extract(null), Is.Empty);
    }

    [Test]
    public void SkillDictionary_KnowsCategories()
    {
        Assert.That(SkillDictionary.CategoryOf("c#"), Is.EqualTo(SkillCategory.Language));
        Assert.That(SkillDictionary.CategoryOf("not-a-skill"), Is.EqualTo(SkillCategory.Other));
        Assert.That(SkillDictionary.Skills.Count, Is.GreaterThan(80));
    }
}
