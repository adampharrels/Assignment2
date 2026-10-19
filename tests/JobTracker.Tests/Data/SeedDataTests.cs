using JobTracker.Analytics.Text;
using JobTracker.Core.Analytics;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Data;
using JobTracker.Data.Seed;
using JobTracker.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Tests.Data;

[TestFixture]
public class SeedDataTests
{
    private static readonly IReadOnlyList<SeedApplication> Applications = SeedData.CreateApplications();

    private static IEnumerable<SeedApplication> Submitted => Applications.Where(application => application.DateApplied.HasValue);

    [Test]
    public void CreateApplications_HasFiftyPlusSubmittedAcrossFifteenCompanies()
    {
        IReadOnlyList<Company> companies = SeedData.CreateCompanies();
        var companyNames = companies.Select(company => company.Name).ToHashSet();

        Assert.That(Submitted.Count(), Is.GreaterThanOrEqualTo(50));
        Assert.That(companies, Has.Count.EqualTo(15));
        Assert.That(companyNames, Has.Count.EqualTo(15), "Company names must be unique.");
        Assert.That(Applications.Select(application => application.CompanyName).Distinct(), Is.EquivalentTo(companyNames));
        Assert.That(Applications.Select(application => application.Id), Is.Unique);
    }

    [Test]
    public void CreateApplications_IsTheSameEveryTime()
    {
        IReadOnlyList<SeedApplication> again = SeedData.CreateApplications();

        Assert.That(again.Select(application => application.JobDescription), Is.EqualTo(Applications.Select(application => application.JobDescription)));
        Assert.That(again.Select(application => application.DateApplied), Is.EqualTo(Applications.Select(application => application.DateApplied)));
        Assert.That(again.Select(application => application.Status), Is.EqualTo(Applications.Select(application => application.Status)));
    }

    [Test]
    public void CreateApplications_HasVariedDetails()
    {
        Assert.That(Submitted.Select(application => application.Source).Distinct().Count(), Is.GreaterThanOrEqualTo(5));
        Assert.That(Submitted.Select(application => application.Location).Distinct().Count(), Is.GreaterThanOrEqualTo(5));
        Assert.That(Submitted.Select(application => application.WorkMode).Distinct(), Is.EquivalentTo(new[] { "On-site", "Hybrid", "Remote" }));
        Assert.That(Submitted.Select(application => application.RoleTitle).Distinct().Count(), Is.GreaterThanOrEqualTo(8));
        Assert.That(Submitted.Select(application => application.DateApplied!.Value.Month).Distinct().Count(), Is.GreaterThanOrEqualTo(4));
        Assert.That(Submitted.Any(application => application.SalaryMin == null), Is.True, "Some ads should not state a salary.");
        Assert.That(Submitted.Select(application => application.SalaryMin).Distinct().Count(), Is.GreaterThanOrEqualTo(10));
    }

    [Test]
    public void CreateApplications_HasAMixOfOutcomesAndTactics()
    {
        var records = Submitted.Select(application => application.ToRecord()).ToList();

        Assert.That(records.Any(r => r.Outcome == ApplicationOutcome.Rejected && r.HighestStage >= PipelineStage.Interview), Is.True, "Rejected after interview");
        Assert.That(records.Any(r => r.Outcome == ApplicationOutcome.Rejected && r.HighestStage == PipelineStage.Applied), Is.True, "Rejected straight away");
        Assert.That(records.Any(r => r.Outcome == ApplicationOutcome.Ghosted), Is.True);
        Assert.That(records.Any(r => r.Outcome == ApplicationOutcome.Withdrawn), Is.True);
        Assert.That(records.Any(r => r.Outcome == ApplicationOutcome.Accepted), Is.True);
        Assert.That(records.Any(r => r.Outcome == ApplicationOutcome.InProgress), Is.True);
        Assert.That(Submitted.Any(a => a.History.Any(e => e.FromStatus == ApplicationStatus.Ghosted)), Is.True, "A ghosted application revived");
        Assert.That(records.Count(r => r.HadReferral), Is.GreaterThanOrEqualTo(5));
        Assert.That(records.Count(r => r.TailoredResume), Is.GreaterThanOrEqualTo(10));
        Assert.That(records.Count(r => r.WroteCoverLetter), Is.GreaterThanOrEqualTo(5));
    }

    [Test]
    public void CreateApplications_ReferralsAndTailoringRaiseTheInterviewRate()
    {
        var records = Submitted.Select(application => application.ToRecord()).ToList();
        double withTactic = InterviewRate(records.Where(r => r.HadReferral || r.TailoredResume));
        double without = InterviewRate(records.Where(r => !r.HadReferral && !r.TailoredResume));

        Assert.That(withTactic, Is.GreaterThan(without + 0.3));
    }

    [Test]
    public void CreateApplications_DatesAreConsistent()
    {
        foreach (SeedApplication application in Applications)
        {
            DateTime start = application.DateApplied ?? application.History[0].ChangedAt;
            Assert.That(application.DatePosted, Is.LessThanOrEqualTo(start), $"Application {application.Id}");
            Assert.That(application.History.Select(e => e.ChangedAt), Is.Ordered, $"Application {application.Id}");
            Assert.That(application.History[^1].ChangedAt, Is.LessThan(new DateTime(2026, 10, 16)), $"Application {application.Id}");
            if (application.DateFirstResponse.HasValue)
            {
                Assert.That(application.DateFirstResponse.Value, Is.GreaterThan(start), $"Application {application.Id}");
            }

            if (application.SalaryMin.HasValue)
            {
                Assert.That(application.SalaryMax, Is.GreaterThan(application.SalaryMin), $"Application {application.Id}");
            }
        }
    }

    [Test]
    public void CreateApplications_HistoriesFollowTheStatusRules()
    {
        foreach (SeedApplication application in Applications)
        {
            SeedStageEvent first = application.History[0];
            Assert.That(first.FromStatus, Is.Null, $"Application {application.Id}");
            Assert.That(first.ToStatus, Is.EqualTo(application.DateApplied.HasValue ? ApplicationStatus.Applied : ApplicationStatus.Wishlist), $"Application {application.Id}");
            if (application.DateApplied.HasValue)
            {
                Assert.That(first.ChangedAt, Is.EqualTo(application.DateApplied.Value), $"Application {application.Id}");
            }

            for (int i = 1; i < application.History.Count; i++)
            {
                SeedStageEvent change = application.History[i];
                Assert.That(change.FromStatus, Is.EqualTo(application.History[i - 1].ToStatus), $"Application {application.Id}");
                Assert.That(IsLegalMove(change.FromStatus!.Value, change.ToStatus, application.History.Take(i)), Is.True,
                    $"Application {application.Id}: {change.FromStatus} -> {change.ToStatus}");
            }
        }
    }

    [Test]
    public void CreateApplications_JobDescriptionsMentionRealSkills()
    {
        var extractor = new DictionarySkillExtractor();
        var allSkills = new HashSet<string>();

        foreach (SeedApplication application in Applications)
        {
            IReadOnlyList<string> skills = extractor.Extract(application.JobDescription);
            Assert.That(skills.Distinct().Count(), Is.GreaterThanOrEqualTo(4), $"Application {application.Id}");
            allSkills.UnionWith(skills);
        }

        Assert.That(allSkills, Is.SupersetOf(new[] { "react", "python", "sql", "c#", "aws" }));
    }

    [Test]
    public void CreateInterviews_MatchTheirApplications()
    {
        Dictionary<int, SeedApplication> byId = Applications.ToDictionary(application => application.Id);
        IReadOnlyList<Interview> interviews = SeedData.CreateInterviews();

        Assert.That(interviews, Has.Count.GreaterThanOrEqualTo(10));
        Assert.That(interviews.Select(interview => interview.ScheduledAt), Is.Ordered);
        Assert.That(interviews.Any(interview => interview.Outcome == InterviewOutcome.Pending), Is.True);
        foreach (Interview interview in interviews)
        {
            Assert.That(byId.TryGetValue(interview.ApplicationId, out SeedApplication? application), Is.True, $"Interview for {interview.ApplicationId}");
            Assert.That(interview.ScheduledAt.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(interview.DurationMinutes, Is.InRange(5, 600));
            Assert.That(application!.HighestStage, Is.GreaterThanOrEqualTo(PipelineStage.Interview), $"Application {application.Id}");

            DateTime invited = application.History.First(e => e.ToStatus == ApplicationStatus.Interview).ChangedAt;
            Assert.That(interview.ScheduledAt, Is.GreaterThan(invited), $"Application {application.Id}");

            if (interview.Outcome == InterviewOutcome.Pending)
            {
                Assert.That(application.Outcome, Is.EqualTo(ApplicationOutcome.InProgress), $"Application {application.Id}");
            }
            else if (interview.Outcome != InterviewOutcome.Cancelled && application.Outcome != ApplicationOutcome.InProgress)
            {
                Assert.That(interview.ScheduledAt, Is.LessThan(application.History[^1].ChangedAt), $"Application {application.Id}");
            }
        }
    }

    [Test]
    public void CreateInterviews_DoNotOverlap()
    {
        var active = SeedData.CreateInterviews().Where(i => i.Outcome != InterviewOutcome.Cancelled).OrderBy(i => i.ScheduledAt).ToList();

        for (int i = 1; i < active.Count; i++)
        {
            DateTime previousEnd = active[i - 1].ScheduledAt.AddMinutes(active[i - 1].DurationMinutes);
            Assert.That(active[i].ScheduledAt, Is.GreaterThanOrEqualTo(previousEnd), $"Interview at {active[i].ScheduledAt:u}");
        }
    }

    [Test]
    public void ToRecord_RejectsWishlistItems()
    {
        SeedApplication wishlist = Applications.First(application => application.Status == ApplicationStatus.Wishlist);

        Assert.That(() => wishlist.ToRecord(), Throws.InvalidOperationException);
    }

    [Test]
    public void ToRecord_UsesTheFurthestStageNotTheCurrentStatus()
    {
        var history = new List<SeedStageEvent>
        {
            new SeedStageEvent { ToStatus = ApplicationStatus.Applied, ChangedAt = new DateTime(2026, 7, 1) },
            new SeedStageEvent { FromStatus = ApplicationStatus.Applied, ToStatus = ApplicationStatus.Ghosted, ChangedAt = new DateTime(2026, 8, 1) },
            new SeedStageEvent { FromStatus = ApplicationStatus.Ghosted, ToStatus = ApplicationStatus.Interview, ChangedAt = new DateTime(2026, 8, 10) },
            new SeedStageEvent { FromStatus = ApplicationStatus.Interview, ToStatus = ApplicationStatus.Rejected, ChangedAt = new DateTime(2026, 8, 20) }
        };
        var application = new SeedApplication { Id = 1, DateApplied = new DateTime(2026, 7, 1), History = history };

        ApplicationRecord record = application.ToRecord();

        Assert.That(record.HighestStage, Is.EqualTo(PipelineStage.Interview));
        Assert.That(record.Outcome, Is.EqualTo(ApplicationOutcome.Rejected));
        Assert.That(record.DateFirstResponse, Is.EqualTo(new DateTime(2026, 8, 10)), "Ghosting is not a reply.");
    }

    [Test]
    public async Task SeedApplicationDataSource_ReturnsOnlySubmittedApplications()
    {
        IReadOnlyList<ApplicationRecord> records = await new SeedApplicationDataSource().GetApplicationsAsync();

        Assert.That(records.Select(record => record.Id), Is.EquivalentTo(Submitted.Select(application => application.Id)));
    }

    [Test]
    public async Task InitializeAsync_SeedsCompaniesAndInterviewsOnce()
    {
        using var database = new TestDatabase();

        await DatabaseInitializer.InitializeAsync(database);
        await DatabaseInitializer.InitializeAsync(database);

        await using JobTrackerDbContext context = database.CreateDbContext();
        Assert.That(await context.Companies.CountAsync(), Is.EqualTo(SeedData.CreateCompanies().Count));
        Assert.That(await context.Interviews.CountAsync(), Is.EqualTo(SeedData.CreateInterviews().Count));
        Assert.That(await context.UserProfiles.SingleAsync(), Has.Property(nameof(UserProfile.ResumeText)).Not.Empty);
    }

    [Test]
    public void AddDemoDataAsync_RejectsNullContext()
    {
        Assert.That(() => SeedData.AddDemoDataAsync(null!), Throws.ArgumentNullException);
    }

    private static double InterviewRate(IEnumerable<ApplicationRecord> records)
    {
        var list = records.ToList();
        return list.Count(r => r.HighestStage >= PipelineStage.Interview) / (double)list.Count;
    }

    /// <summary>The spec's status rules: forward-only pipeline, exits from any open status, ghosts can be revived.</summary>
    private static bool IsLegalMove(ApplicationStatus from, ApplicationStatus to, IEnumerable<SeedStageEvent> earlier)
    {
        bool fromTerminal = from is ApplicationStatus.Accepted or ApplicationStatus.Rejected or ApplicationStatus.Withdrawn;
        bool toExit = to is ApplicationStatus.Rejected or ApplicationStatus.Withdrawn or ApplicationStatus.Ghosted;
        if (fromTerminal)
        {
            return false;
        }

        if (from == ApplicationStatus.Ghosted)
        {
            ApplicationStatus beforeGhosting = earlier.Last(e => e.ToStatus != ApplicationStatus.Ghosted).ToStatus;
            return to == ApplicationStatus.Rejected || (to <= ApplicationStatus.Accepted && to > beforeGhosting);
        }

        return toExit || (to <= ApplicationStatus.Accepted && to > from);
    }
}
