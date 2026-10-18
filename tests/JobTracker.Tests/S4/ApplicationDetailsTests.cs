using JobTracker.Analytics.Text;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Extensions;
using JobTracker.Data;
using JobTracker.Data.Services;
using JobTracker.Tests.K1;

namespace JobTracker.Tests.S4;

[TestFixture]
public class ApplicationDetailsTests
{
    /// <summary>A tiny fake skill list, so the highlighter tests do not depend on Adam's dictionary.</summary>
    private static readonly Dictionary<string, string> FakeSkills = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["react"] = "react",
        ["c#"] = "c#",
        ["sql"] = "sql",
        ["machine learning"] = "machine learning"
    };

    private static string? FakeSkillOf(string phrase) => FakeSkills.GetValueOrDefault(phrase);

    [Test]
    public void Split_JoiningTheSegments_GivesBackTheOriginalText()
    {
        const string text = "We use React, C# and SQL.\nMachine learning is a plus!";

        IReadOnlyList<TextSegment> segments = SkillHighlighter.Split(text, FakeSkillOf);

        Assert.That(string.Concat(segments.Select(segment => segment.Text)), Is.EqualTo(text));
    }

    [Test]
    public void Split_HighlightsSkillsWithoutTheirPunctuation()
    {
        IReadOnlyList<TextSegment> segments = SkillHighlighter.Split("We use React, C# and SQL.", FakeSkillOf);

        Assert.That(segments.Where(segment => segment.IsSkill).Select(segment => segment.Text),
            Is.EqualTo(new[] { "React", "C#", "SQL" }));
    }

    [Test]
    public void Split_FindsTwoWordSkills_ButNotASkillFollowedByAnotherWord()
    {
        IReadOnlyList<TextSegment> segments = SkillHighlighter.Split("Machine learning with C# and more", FakeSkillOf);

        Assert.That(segments.Where(segment => segment.IsSkill).Select(segment => segment.Text),
            Is.EqualTo(new[] { "Machine learning", "C#" }));
    }

    [Test]
    public void DistinctSkills_ListsEachSkillOnceInOrder()
    {
        IReadOnlyList<TextSegment> segments = SkillHighlighter.Split("SQL then React then sql again", FakeSkillOf);

        Assert.That(SkillHighlighter.DistinctSkills(segments), Is.EqualTo(new[] { "sql", "react" }));
    }

    [TestCase(null)]
    [TestCase("")]
    public void Split_EmptyText_GivesNoSegments(string? text)
    {
        Assert.That(SkillHighlighter.Split(text, FakeSkillOf), Is.Empty);
    }

    [Test]
    public void Split_WithAdamsSkillExtractor_HighlightsRealSkills()
    {
        var extractor = new DictionarySkillExtractor();

        IReadOnlyList<TextSegment> segments = SkillHighlighter.Split(
            "Build REST APIs in C# and .NET with SQL Server.",
            phrase => extractor.Extract(phrase) is { Count: 1 } found ? found[0] : null);

        Assert.That(SkillHighlighter.DistinctSkills(segments), Does.Contain("c#"));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsTheTimelineOldestFirst()
    {
        using var database = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(database);
        var service = new ApplicationService(database);

        JobApplication created = await service.CreateAsync(new JobApplication
        {
            RoleTitle = "Graduate Developer",
            Location = "Sydney",
            Status = ApplicationStatus.Applied,
            DateApplied = new DateTime(2026, 9, 1)
        }, "Canva");
        await service.ChangeStatusAsync(created.Id, ApplicationStatus.Assessment);
        await service.ChangeStatusAsync(created.Id, ApplicationStatus.Interview);

        JobApplication details = (await service.GetByIdAsync(created.Id))!;

        Assert.That(details.StageHistory.Select(stageEvent => stageEvent.ToStatus),
            Is.EqualTo(new[] { ApplicationStatus.Applied, ApplicationStatus.Assessment, ApplicationStatus.Interview }));
    }
}
