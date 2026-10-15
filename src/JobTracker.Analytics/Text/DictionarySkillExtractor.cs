namespace JobTracker.Analytics.Text;

/// <summary>
/// Finds skills by looking up the text's words in the SkillDictionary. It tries the longest phrase
/// first, so "sql server" is read as one skill rather than "sql" plus the word "server", and
/// "amazon web services" becomes "aws".
/// </summary>
public class DictionarySkillExtractor : ISkillExtractor
{
    /// <inheritdoc />
    public IReadOnlyList<string> Extract(string? text)
    {
        List<string> tokens = Tokenizer.Tokenize(text).ToList();
        var skills = new List<string>();

        int position = 0;
        while (position < tokens.Count)
        {
            int matchedLength = 0;
            for (int length = Math.Min(SkillDictionary.LongestPhraseLength, tokens.Count - position); length >= 1; length--)
            {
                string phrase = string.Join(" ", tokens.Skip(position).Take(length));
                string? skill = SkillDictionary.FindSkill(phrase);
                if (skill != null)
                {
                    skills.Add(skill);
                    matchedLength = length;
                    break;
                }
            }

            position += matchedLength > 0 ? matchedLength : 1;
        }

        return skills;
    }
}
