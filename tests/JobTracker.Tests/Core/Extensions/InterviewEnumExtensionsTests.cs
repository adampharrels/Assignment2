using JobTracker.Core.Enums;
using JobTracker.Core.Extensions;

namespace JobTracker.Tests.Core.Extensions;

[TestFixture]
public class InterviewEnumExtensionsTests
{
    [TestCase(InterviewType.FinalRound, "Final round")]
    [TestCase(InterviewType.AssessmentCentre, "Assessment centre")]
    [TestCase(InterviewType.Technical, "Technical")]
    public void ToDisplayText_GivesReadableLabels(InterviewType type, string expected)
    {
        Assert.That(type.ToDisplayText(), Is.EqualTo(expected));
    }
}
