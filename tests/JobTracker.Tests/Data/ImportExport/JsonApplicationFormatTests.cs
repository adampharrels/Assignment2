using System.Text.Json;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Data.ImportExport;

namespace JobTracker.Tests.Data.ImportExport;

[TestFixture]
public class JsonApplicationFormatTests
{
    private readonly JsonApplicationFormat _format = new JsonApplicationFormat();

    [Test]
    public void ExportThenParse_GivesBackTheSameApplications()
    {
        JobApplication original = CsvApplicationFormatTests.FullApplication();

        ParseResult result = _format.Parse(_format.Export(new[] { original }));

        Assert.That(result.FileError, Is.Null);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Applications, Has.Count.EqualTo(1));
        CsvApplicationFormatTests.AssertSameDetails(result.Applications[0], original);
    }

    [Test]
    public void Export_WritesNumbersAndYesNoAsJsonValues()
    {
        string json = _format.Export(new[] { CsvApplicationFormatTests.FullApplication() });

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        JsonElement application = root.GetProperty("applications")[0];
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("format").GetString(), Is.EqualTo(JsonApplicationFormat.FormatId));
            Assert.That(application.GetProperty("salaryMin").GetDecimal(), Is.EqualTo(85_000m));
            Assert.That(application.GetProperty("referral").GetBoolean(), Is.True);
            Assert.That(application.GetProperty("coverLetter").GetBoolean(), Is.False);
            Assert.That(application.GetProperty("dateApplied").GetString(), Is.EqualTo("2026-09-01"));
            Assert.That(application.GetProperty("status").GetString(), Is.EqualTo("Final round"));
        });
    }

    [Test]
    public void Parse_PlainArray_IsAccepted()
    {
        const string json = """
            [
              { "company": "Canva", "role": "Engineer", "location": "Sydney", "status": "Applied", "dateApplied": "2026-09-01", "referral": true },
            ]
            """;

        ParseResult result = _format.Parse(json);

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Applications[0].Application.HadReferral, Is.True);
        Assert.That(result.Applications[0].Application.Status, Is.EqualTo(ApplicationStatus.Applied));
    }

    [Test]
    public void Parse_BadEntries_AreListedByPositionAndGoodOnesKept()
    {
        const string json = """
            { "applications": [
                { "company": "Canva", "role": "Engineer", "location": "Sydney" },
                "not an application",
                { "company": "Xero", "role": "Engineer", "location": "Remote", "salaryMin": 90000, "salaryMax": 80000 },
                { "company": "NAB", "role": "Analyst", "location": "Melbourne", "dateApplied": "2999-01-01", "status": "Applied" }
            ] }
            """;

        ParseResult result = _format.Parse(json);

        Assert.That(result.Applications.Select(row => row.RowNumber), Is.EqualTo(new[] { 1 }));
        Assert.That(result.Errors.Select(error => error.RowNumber), Is.EqualTo(new[] { 2, 3, 4 }));
        Assert.Multiple(() =>
        {
            Assert.That(result.Errors[0].Message, Does.Contain("not an application"));
            Assert.That(result.Errors[1].Message, Does.Contain("Minimum salary can't be more than the maximum"));
            Assert.That(result.Errors[2].Message, Does.Contain("can't be in the future"));
        });
    }

    [TestCase("{ not json", "not valid JSON")]
    [TestCase("{ \"something\": 1 }", "list of applications")]
    [TestCase("[]", "no applications")]
    public void Parse_UnreadableFile_IsAFileError(string json, string expected)
    {
        ParseResult result = _format.Parse(json);

        Assert.That(result.FileError, Does.Contain(expected));
        Assert.That(result.Applications, Is.Empty);
    }

    [Test]
    public void ExportThenParse_AWishlistItemStaysOnTheWishlist()
    {
        var wishlist = new JobApplication
        {
            Company = new Company { Name = "Atlassian" },
            RoleTitle = "Graduate Engineer",
            Location = "Remote",
            Status = ApplicationStatus.Wishlist
        };

        ParsedApplication parsed = _format.Parse(_format.Export(new[] { wishlist })).Applications.Single();

        Assert.That(parsed.Application.Status, Is.EqualTo(ApplicationStatus.Wishlist));
        Assert.That(parsed.Application.DateApplied, Is.Null);
        Assert.That(parsed.Application.SalaryMin, Is.Null);
    }
}
