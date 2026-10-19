using System.Text;

namespace JobTracker.Analytics.Text;

/// <summary>
/// Splits job descriptions into lower-case words ("tokens") for text analysis.
/// Unlike a plain split on punctuation, it keeps tech terms whole: "c#", "c++", ".net",
/// "node.js", "ci/cd" and "scikit-learn" each stay one token. Common English words are dropped.
/// </summary>
public static class Tokenizer
{
    /// <summary>Words that carry no meaning for skill analysis.</summary>
    private static readonly HashSet<string> StopWords = new HashSet<string>
    {
        "a", "about", "above", "across", "after", "all", "also", "an", "and", "any", "are", "as", "at",
        "be", "been", "being", "both", "but", "by", "can", "day", "do", "e.g", "each", "every", "for",
        "from", "have", "has", "help", "how", "i.e", "if", "in", "including", "into", "is", "it", "its",
        "join", "like", "looking", "more", "most", "must", "new", "nice", "not", "of", "on", "one", "or",
        "other", "our", "out", "over", "own", "plus", "role", "so", "such", "than", "that", "the",
        "their", "them", "then", "there", "these", "they", "this", "those", "through", "to", "up",
        "us", "use", "using", "very", "want", "was", "we", "well", "were", "what", "when", "where",
        "which", "who", "will", "with", "within", "work", "would", "you", "your"
    };

    /// <summary>Returns the tokens of the text one at a time, in order. Empty text gives no tokens.</summary>
    public static IEnumerable<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var current = new StringBuilder();
        foreach (char character in text.ToLowerInvariant())
        {
            if (IsTokenCharacter(character))
            {
                current.Append(character);
                continue;
            }

            foreach (string token in CleanTokens(current.ToString()))
            {
                yield return token;
            }

            current.Clear();
        }

        foreach (string token in CleanTokens(current.ToString()))
        {
            yield return token;
        }
    }

    /// <summary>Letters and digits, plus the symbols that appear inside tech terms.</summary>
    private static bool IsTokenCharacter(char character)
    {
        return char.IsLetterOrDigit(character)
            || character == '#' || character == '+' || character == '.' || character == '/' || character == '-';
    }

    /// <summary>
    /// Tidies one raw chunk: removes sentence punctuation around it, splits "react/angular" into two
    /// words but keeps short pairs like "ci/cd" and "a/b", and drops stop words and plain numbers.
    /// </summary>
    private static IEnumerable<string> CleanTokens(string raw)
    {
        string chunk = raw.Trim('-', '/').TrimEnd('.');
        if (chunk.Length == 0)
        {
            yield break;
        }

        string[] parts = chunk.Split('/', StringSplitOptions.RemoveEmptyEntries);
        bool keepSlash = parts.Length == 2 && parts[0].Length <= 2 && parts[1].Length <= 2;
        IEnumerable<string> words = keepSlash ? new[] { chunk } : parts;

        foreach (string word in words)
        {
            string token = word.Trim('-').TrimEnd('.');
            if (token.Length == 0 || token == "." || StopWords.Contains(token) || IsNumber(token))
            {
                continue;
            }

            yield return token;
        }
    }

    private static bool IsNumber(string token)
    {
        return token.All(character => char.IsDigit(character) || character == '.' || character == ',');
    }
}
