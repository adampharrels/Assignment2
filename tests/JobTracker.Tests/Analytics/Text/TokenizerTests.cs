using JobTracker.Analytics.Text;

namespace JobTracker.Tests.Analytics.Text;

[TestFixture]
public class TokenizerTests
{
    [Test]
    public void Tokenize_KeepsTechTermsWhole()
    {
        List<string> tokens = Tokenizer.Tokenize("Experience with C#, .NET, Node.js, C++ and CI/CD.").ToList();

        Assert.That(tokens, Is.EqualTo(new[] { "experience", "c#", ".net", "node.js", "c++", "ci/cd" }));
    }

    [Test]
    public void Tokenize_SplitsLongSlashPairs()
    {
        List<string> tokens = Tokenizer.Tokenize("React/Angular").ToList();

        Assert.That(tokens, Is.EqualTo(new[] { "react", "angular" }));
    }

    [Test]
    public void Tokenize_DropsStopWordsAndNumbers()
    {
        List<string> tokens = Tokenizer.Tokenize("You will work with the team for 2 years").ToList();

        Assert.That(tokens, Is.EqualTo(new[] { "team", "years" }));
    }

    [Test]
    public void Tokenize_EmptyOrNull_GivesNoTokens()
    {
        Assert.That(Tokenizer.Tokenize(null), Is.Empty);
        Assert.That(Tokenizer.Tokenize("   "), Is.Empty);
    }
}
