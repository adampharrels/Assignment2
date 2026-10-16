using System.Globalization;
using JobTracker.Analytics.Statistics;

namespace JobTracker.Tests.A1;

[TestFixture]
public class TextFormatTests
{
    /// <summary>A culture that writes "50 %" and "2,0": the cases that made CI fail before TextFormat.</summary>
    private static CultureInfo AwkwardCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.PercentDecimalSeparator = ",";
        culture.NumberFormat.PercentPositivePattern = 0; // "n %"
        return culture;
    }

    [Test]
    public void Formats_AreTheSameWhateverTheMachineCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            foreach (CultureInfo culture in new[] { CultureInfo.InvariantCulture, AwkwardCulture() })
            {
                CultureInfo.CurrentCulture = culture;

                Assert.That(TextFormat.Percent(0.5), Is.EqualTo("50%"));
                Assert.That(TextFormat.Times(2.04), Is.EqualTo("2.0x"));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
