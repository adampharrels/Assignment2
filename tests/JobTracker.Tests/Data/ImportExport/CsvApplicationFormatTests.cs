using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Data.ImportExport;

namespace JobTracker.Tests.Data.ImportExport;

[TestFixture]
public class CsvApplicationFormatTests
{
    private const string Header = "Company,Role,Location,Status,Date applied";

    private readonly CsvApplicationFormat _format = new CsvApplicationFormat();

    [Test]
    public void ExportThenParse_GivesBackTheSameApplications()
    {
        JobApplication original = FullApplication();

        ParseResult result = _format.Parse(_format.Export(new[] { original }));

        Assert.That(result.FileError, Is.Null);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Applications, Has.Count.EqualTo(1));
        AssertSameDetails(result.Applications[0], original);
    }

    [Test]
    public void Export_QuotesCommasQuotesAndLineBreaks()
    {
        JobApplication application = FullApplication();
        application.Notes = "Spoke to \"Sam\", the hiring manager.\nFollow up Friday.";

        string csv = _format.Export(new[] { application });
        ParseResult result = _format.Parse(csv);

        Assert.That(csv, Does.Contain("\"Spoke to \"\"Sam\"\", the hiring manager.\nFollow up Friday.\""));
        Assert.That(result.Applications[0].Application.Notes, Is.EqualTo(application.Notes));
    }

    [Test]
    public void Export_NoApplications_WritesOnlyTheHeadings()
    {
        string csv = _format.Export(Array.Empty<JobApplication>());

        Assert.That(csv.TrimStart('﻿').TrimEnd(), Is.EqualTo(string.Join(",", ApplicationColumns.All.Select(column => column.Header))));
    }

    [Test]
    public void Parse_BadRows_AreListedWithSpreadsheetRowNumbersAndGoodRowsKept()
    {
        string csv = string.Join("\n",
            Header,
            "Canva,Graduate Engineer,Sydney,Applied,2026-09-01",      // row 2: good
            "Atlassian,,Sydney,Applied,2026-09-02",                  // row 3: no role
            "Xero,Graduate Developer,Remote,Hired,2026-09-03",       // row 4: unknown status
            "NAB,Data Analyst,Melbourne,Interview,not a date",       // row 5: bad date
            "Telstra,Cloud Engineer,Sydney,Applied,1/9/2026");       // row 6: good, day-first date

        ParseResult result = _format.Parse(csv);

        Assert.That(result.Applications.Select(row => row.RowNumber), Is.EqualTo(new[] { 2, 6 }));
        Assert.That(result.Errors.Select(error => error.RowNumber), Is.EqualTo(new[] { 3, 4, 5 }));
        Assert.Multiple(() =>
        {
            Assert.That(result.Errors[0].Message, Does.Contain("Role is required"));
            Assert.That(result.Errors[1].Message, Does.Contain("Status \"Hired\""));
            Assert.That(result.Errors[2].Message, Does.Contain("not a date"));
            Assert.That(result.Applications[1].Application.DateApplied, Is.EqualTo(new DateTime(2026, 9, 1)));
        });
    }

    [Test]
    public void Parse_RowWithSeveralProblems_ListsThemAll()
    {
        string csv = Header + ",Salary min\n,Engineer,Sydney,Offer,,lots";

        ParseResult result = _format.Parse(csv);

        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0].Message, Does.Contain("Company is required"));
        Assert.That(result.Errors[0].Message, Does.Contain("Salary min \"lots\" is not a number"));
        Assert.That(result.Errors[0].Message, Does.Contain("Applied date is required"));
    }

    [Test]
    public void Parse_HeadingsInAnyCaseAndOrder_UnknownColumnsIgnoredAndBlankLinesSkipped()
    {
        string csv = "LOCATION,role,Favourite colour,COMPANY,work_mode\r\nSydney,Engineer,Blue,Canva,remote\r\n\r\n,,,,\r\n";

        ParseResult result = _format.Parse(csv);

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Applications, Has.Count.EqualTo(1));
        Assert.That(result.Applications[0].CompanyName, Is.EqualTo("Canva"));
        Assert.That(result.Applications[0].Application.WorkMode, Is.EqualTo(WorkMode.Remote));
        Assert.That(result.Applications[0].Application.Status, Is.EqualTo(ApplicationStatus.Wishlist), "No status and no applied date means wishlist.");
    }

    [Test]
    public void Parse_MissingRequiredHeading_IsAFileError()
    {
        ParseResult result = _format.Parse("Company,Role\nCanva,Engineer");

        Assert.That(result.FileError, Does.Contain("Location"));
        Assert.That(result.Applications, Is.Empty);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("Company,Role,Location\n")]
    public void Parse_NothingToImport_IsAFileError(string csv)
    {
        Assert.That(_format.Parse(csv).FileError, Is.Not.Null);
    }

    [Test]
    public void Parse_UnclosedQuote_IsAFileError()
    {
        ParseResult result = _format.Parse("Company,Role,Location\n\"Canva,Engineer,Sydney");

        Assert.That(result.FileError, Does.Contain("never closed"));
    }

    [Test]
    public void Matches_ChecksTheExtensionIgnoringCase()
    {
        Assert.That(_format.Matches("My Applications.CSV"), Is.True);
        Assert.That(_format.Matches("backup.json"), Is.False);
    }

    /// <summary>An application with every column filled in.</summary>
    internal static JobApplication FullApplication() => new JobApplication
    {
        Company = new Company { Name = "Canva" },
        RoleTitle = "Graduate Software Engineer",
        Location = "Sydney",
        WorkMode = WorkMode.Hybrid,
        Source = ApplicationSource.CareerFair,
        Status = ApplicationStatus.FinalRound,
        SalaryMin = 85_000,
        SalaryMax = 95_500.50m,
        DatePosted = new DateTime(2026, 8, 20),
        DateApplied = new DateTime(2026, 9, 1),
        HadReferral = true,
        TailoredResume = true,
        WroteCoverLetter = false,
        JobUrl = "https://www.canva.com/careers/grad",
        JobDescription = "Build features in React, TypeScript and C#.",
        Notes = "Met the team at the UTS careers fair."
    };

    /// <summary>Checks every column survived the trip.</summary>
    internal static void AssertSameDetails(ParsedApplication parsed, JobApplication original)
    {
        JobApplication copy = parsed.Application;
        Assert.Multiple(() =>
        {
            Assert.That(parsed.CompanyName, Is.EqualTo(original.Company!.Name));
            Assert.That(copy.RoleTitle, Is.EqualTo(original.RoleTitle));
            Assert.That(copy.Location, Is.EqualTo(original.Location));
            Assert.That(copy.WorkMode, Is.EqualTo(original.WorkMode));
            Assert.That(copy.Source, Is.EqualTo(original.Source));
            Assert.That(copy.Status, Is.EqualTo(original.Status));
            Assert.That(copy.SalaryMin, Is.EqualTo(original.SalaryMin));
            Assert.That(copy.SalaryMax, Is.EqualTo(original.SalaryMax));
            Assert.That(copy.DatePosted, Is.EqualTo(original.DatePosted));
            Assert.That(copy.DateApplied, Is.EqualTo(original.DateApplied));
            Assert.That(copy.HadReferral, Is.EqualTo(original.HadReferral));
            Assert.That(copy.TailoredResume, Is.EqualTo(original.TailoredResume));
            Assert.That(copy.WroteCoverLetter, Is.EqualTo(original.WroteCoverLetter));
            Assert.That(copy.JobUrl, Is.EqualTo(original.JobUrl));
            Assert.That(copy.JobDescription, Is.EqualTo(original.JobDescription));
            Assert.That(copy.Notes, Is.EqualTo(original.Notes));
        });
    }
}
