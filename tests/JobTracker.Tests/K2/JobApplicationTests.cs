using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Analytics;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Extensions;

namespace JobTracker.Tests.K2;

[TestFixture]
public class JobApplicationTests
{
    [Test]
    public void Validate_ValidApplication_HasNoErrors()
    {
        Assert.That(ValidationErrors(CreateValidApplication()), Is.Empty);
    }

    [Test]
    public void Validate_MissingRoleAndLocation_ReportsBothFields()
    {
        JobApplication application = CreateValidApplication();
        application.RoleTitle = string.Empty;
        application.Location = string.Empty;

        List<ValidationResult> errors = ValidationErrors(application);

        Assert.That(errors.SelectMany(error => error.MemberNames), Is.SupersetOf(new[] { "RoleTitle", "Location" }));
    }

    [Test]
    public void Validate_SalaryMinAboveMax_IsRejected()
    {
        JobApplication application = CreateValidApplication();
        application.SalaryMin = 100_000;
        application.SalaryMax = 90_000;

        Assert.That(ValidationErrors(application).SelectMany(error => error.MemberNames), Does.Contain("SalaryMin"));
    }

    [Test]
    public void Validate_AppliedDateInTheFuture_IsRejected()
    {
        JobApplication application = CreateValidApplication();
        application.DateApplied = DateTime.Today.AddDays(1);

        Assert.That(ValidationErrors(application).SelectMany(error => error.MemberNames), Does.Contain("DateApplied"));
    }

    [Test]
    public void Validate_InvalidUrl_IsRejected()
    {
        JobApplication application = CreateValidApplication();
        application.JobUrl = "not a url";

        Assert.That(ValidationErrors(application).SelectMany(error => error.MemberNames), Does.Contain("JobUrl"));
    }

    [Test]
    public void ToRecord_RejectedAfterInterview_StillCountsTheInterview()
    {
        JobApplication application = CreateValidApplication();
        application.Status = ApplicationStatus.Rejected;
        application.StageHistory = new List<StageEvent>
        {
            new StageEvent { FromStatus = null, ToStatus = ApplicationStatus.Applied, ChangedAt = new DateTime(2026, 9, 1) },
            new StageEvent { FromStatus = ApplicationStatus.Applied, ToStatus = ApplicationStatus.Interview, ChangedAt = new DateTime(2026, 9, 8) },
            new StageEvent { FromStatus = ApplicationStatus.Interview, ToStatus = ApplicationStatus.Rejected, ChangedAt = new DateTime(2026, 9, 20) }
        };

        ApplicationRecord record = application.ToRecord();

        Assert.Multiple(() =>
        {
            Assert.That(record.HighestStage, Is.EqualTo(PipelineStage.Interview));
            Assert.That(record.Outcome, Is.EqualTo(ApplicationOutcome.Rejected));
            Assert.That(record.DateFirstResponse, Is.EqualTo(new DateTime(2026, 9, 8)));
            Assert.That(record.CompanyName, Is.EqualTo("Canva"));
            Assert.That(record.Source, Is.EqualTo("Career fair"));
            Assert.That(record.WorkMode, Is.EqualTo("On-site"));
        });
    }

    [Test]
    public void ToRecord_WishlistItem_Throws()
    {
        JobApplication application = CreateValidApplication();
        application.Status = ApplicationStatus.Wishlist;
        application.DateApplied = null;

        Assert.Throws<InvalidOperationException>(() => application.ToRecord());
    }

    [TestCase("Career fair", ApplicationSource.CareerFair)]
    [TestCase("careerfair", ApplicationSource.CareerFair)]
    [TestCase("LinkedIn", ApplicationSource.LinkedIn)]
    [TestCase("Company website", ApplicationSource.CompanyWebsite)]
    public void TryParseSource_DisplayTextOrName_Matches(string text, ApplicationSource expected)
    {
        bool parsed = ApplicationEnumExtensions.TryParseSource(text, out ApplicationSource source);

        Assert.That(parsed, Is.True);
        Assert.That(source, Is.EqualTo(expected));
    }

    [Test]
    public void TryParseWorkMode_UnknownText_ReturnsFalse()
    {
        Assert.That(ApplicationEnumExtensions.TryParseWorkMode("on the moon", out _), Is.False);
    }

    /// <summary>A submitted application that passes every rule.</summary>
    private static JobApplication CreateValidApplication() => new JobApplication
    {
        CompanyId = 1,
        Company = new Company { Id = 1, Name = "Canva" },
        RoleTitle = "Graduate Software Engineer",
        Location = "Sydney",
        WorkMode = WorkMode.OnSite,
        Source = ApplicationSource.CareerFair,
        Status = ApplicationStatus.Applied,
        SalaryMin = 85_000,
        SalaryMax = 95_000,
        JobUrl = "https://www.canva.com/careers/",
        DatePosted = new DateTime(2026, 8, 25),
        DateApplied = new DateTime(2026, 9, 1)
    };

    /// <summary>Runs the same validation the application service will run.</summary>
    private static List<ValidationResult> ValidationErrors(JobApplication application)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(application, new ValidationContext(application), results, validateAllProperties: true);
        return results;
    }
}
