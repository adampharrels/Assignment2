using System.Text.RegularExpressions;

namespace JobTracker.Core.Extensions;

/// <summary>A piece of a job description: plain text, or a recognised skill to highlight.</summary>
/// <param name="Text">The original text, exactly as it appears.</param>
/// <param name="Skill">The skill's canonical name when this piece is a skill, otherwise null.</param>
public record TextSegment(string Text, string? Skill)
{
    /// <summary>True when this piece should be highlighted.</summary>
    public bool IsSkill => Skill != null;
}

/// <summary>
/// Splits a job description into plain text and skill mentions, so the details page (S4) can highlight
/// the skills. Which words are skills is decided by the function passed in (the app uses Adam's skill
/// extractor), so this class does not depend on the analytics project and is easy to test.
/// </summary>
public static partial class SkillHighlighter
{
    /// <summary>Splits text into words and the whitespace between them, keeping both.</summary>
    [GeneratedRegex(@"(\s+)")]
    private static partial Regex WordsAndSpaces();

    /// <summary>Punctuation that can stick to a word without being part of a skill name.</summary>
    private static readonly char[] Punctuation = { ',', '.', ';', ':', '!', '?', '(', ')', '"', '\'' };

    /// <summary>Splits the text into segments; joining every segment's Text gives back the original text.</summary>
    /// <param name="text">The job description.</param>
    /// <param name="skillOf">Returns the canonical skill for a word or two-word phrase, or null if it is not a skill.</param>
    public static IReadOnlyList<TextSegment> Split(string? text, Func<string, string?> skillOf)
    {
        ArgumentNullException.ThrowIfNull(skillOf);

        var segments = new List<TextSegment>();
        if (string.IsNullOrEmpty(text))
        {
            return segments;
        }

        // Even indexes are words, odd indexes are the whitespace between them.
        string[] parts = WordsAndSpaces().Split(text);
        var plain = new System.Text.StringBuilder();

        for (int index = 0; index < parts.Length; index++)
        {
            string part = parts[index];
            bool isWord = index % 2 == 0 && part.Length > 0;

            // Try a two-word skill first ("machine learning", "power bi"), then a single word.
            if (isWord && index + 2 < parts.Length && parts[index + 1] == " "
                && TryMatchPair(part, parts[index + 2], skillOf, out Match? pair))
            {
                FlushPlain(plain, segments);
                AddMatch(part + parts[index + 1] + parts[index + 2], pair!, segments);
                index += 2;
            }
            else if (isWord && TryMatch(part, skillOf, out Match? single))
            {
                FlushPlain(plain, segments);
                AddMatch(part, single!, segments);
            }
            else
            {
                plain.Append(part);
            }
        }

        FlushPlain(plain, segments);
        return segments;
    }

    /// <summary>The distinct skills found in the text, in the order they first appear.</summary>
    public static IReadOnlyList<string> DistinctSkills(IEnumerable<TextSegment> segments) =>
        segments.Where(segment => segment.IsSkill).Select(segment => segment.Skill!).Distinct().ToList();

    /// <summary>A recognised skill, plus how much punctuation surrounds it in the original text.</summary>
    private sealed record Match(string Skill, int Leading, int Trailing);

    /// <summary>
    /// A two-word phrase counts only when it is a skill that neither word is on its own, so "machine learning"
    /// matches but "C# and" does not (that is just "C#" followed by another word).
    /// </summary>
    private static bool TryMatchPair(string first, string second, Func<string, string?> skillOf, out Match? match)
    {
        if (TryMatch(first + " " + second, skillOf, out match)
            && !(TryMatch(first, skillOf, out Match? alone) && alone!.Skill == match!.Skill)
            && !(TryMatch(second, skillOf, out Match? secondAlone) && secondAlone!.Skill == match!.Skill))
        {
            return true;
        }

        match = null;
        return false;
    }

    /// <summary>Checks the phrase as written, then without surrounding punctuation (so "React," still matches).</summary>
    private static bool TryMatch(string phrase, Func<string, string?> skillOf, out Match? match)
    {
        string? skill = skillOf(phrase);
        if (skill != null)
        {
            match = new Match(skill, 0, 0);
            return true;
        }

        string trimmed = phrase.Trim(Punctuation);
        if (trimmed.Length > 0 && trimmed != phrase && skillOf(trimmed) is string trimmedSkill)
        {
            int leading = phrase.IndexOf(trimmed, StringComparison.Ordinal);
            match = new Match(trimmedSkill, leading, phrase.Length - leading - trimmed.Length);
            return true;
        }

        match = null;
        return false;
    }

    /// <summary>Adds the skill as its own segment, with any surrounding punctuation as plain text.</summary>
    private static void AddMatch(string original, Match match, List<TextSegment> segments)
    {
        if (match.Leading > 0)
        {
            segments.Add(new TextSegment(original[..match.Leading], null));
        }

        segments.Add(new TextSegment(original[match.Leading..(original.Length - match.Trailing)], match.Skill));

        if (match.Trailing > 0)
        {
            segments.Add(new TextSegment(original[^match.Trailing..], null));
        }
    }

    private static void FlushPlain(System.Text.StringBuilder plain, List<TextSegment> segments)
    {
        if (plain.Length > 0)
        {
            segments.Add(new TextSegment(plain.ToString(), null));
            plain.Clear();
        }
    }
}
