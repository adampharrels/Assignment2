using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using System.Text;
using JobTracker.Data;
using JobTracker.Data.Profile;
using JobTracker.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Tests.Data.Services;

[TestFixture]
public class DemoDataServiceTests
{
    private TestDatabase _database = null!;
    private DemoDataService _service = null!;
    private ProfileService _profile = null!;
    private ApplicationService _applications = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(_database);
        _service = new DemoDataService(_database);
        _profile = new ProfileService(_database);
        _applications = new ApplicationService(_database);
    }

    [TearDown]
    public void TearDown()
    {
        _database.Dispose();
    }

    [Test]
    public async Task ResetAsync_UndoesProfileAndSkillChanges()
    {
        UserProfile original = await _profile.GetProfileAsync();
        IReadOnlyList<string> originalSkills = await _profile.GetProfileSkillNamesAsync();

        UserProfile changed = await _profile.GetProfileAsync();
        changed.FullName = "Jess Nguyen";
        changed.ResumeText = "Only knows COBOL.";
        await _profile.SaveProfileAsync(changed);
        foreach (Skill skill in await _profile.GetSkillsAsync())
        {
            await _profile.SetInProfileAsync(skill.Id, !skill.InMyProfile);
        }

        await _service.ResetAsync();

        UserProfile restored = await _profile.GetProfileAsync();
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(restored.FullName, Is.EqualTo(original.FullName));
            Assert.That(restored.ResumeText, Is.EqualTo(original.ResumeText));
            Assert.That(await _profile.GetProfileSkillNamesAsync(), Is.EqualTo(originalSkills));
        });
    }

    [Test]
    public async Task ResetAsync_RemovesAddedApplicationsAndCompanies()
    {
        await _applications.CreateAsync(new JobApplication
        {
            RoleTitle = "Graduate Engineer",
            Location = "Sydney",
            Status = ApplicationStatus.Applied,
            DateApplied = DateTime.Today
        }, "Brand New Co");

        await _service.ResetAsync();

        await using JobTrackerDbContext context = _database.CreateDbContext();
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await context.Applications.CountAsync(), Is.Zero);
            Assert.That(await context.StageEvents.CountAsync(), Is.Zero);
            Assert.That(await context.Companies.AnyAsync(company => company.Name == "Brand New Co"), Is.False);
        });
    }

    [Test]
    public async Task ResetAsync_RemovesTheUploadedResume()
    {
        string directory = Path.Combine(Path.GetTempPath(), "jobtracker-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var resumes = new ResumeFileStore(directory);
            using (var pdf = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF\n")))
            {
                await resumes.SaveAsync(pdf, "resume.pdf");
            }

            await new DemoDataService(_database, resumes).ResetAsync();

            Assert.That(resumes.Get(), Is.Null);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    public async Task ResetAsync_RestoresTheSameDemoDataAsFirstRun()
    {
        await using (JobTrackerDbContext before = _database.CreateDbContext())
        {
            // Delete some demo rows, as a user might.
            await before.Companies.Where(company => company.Name == "Canva").ExecuteDeleteAsync();
            await before.Interviews.Take(5).ExecuteDeleteAsync();
        }

        await _service.ResetAsync();
        await _service.ResetAsync(); // twice in a row still gives one copy

        await using JobTrackerDbContext context = _database.CreateDbContext();
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await context.Companies.CountAsync(), Is.EqualTo(SeedData.CreateCompanies().Count));
            Assert.That(await context.Interviews.CountAsync(), Is.EqualTo(SeedData.CreateInterviews().Count));
            Assert.That(await context.UserProfiles.CountAsync(), Is.EqualTo(1));
            Assert.That(await context.Skills.CountAsync(), Is.GreaterThan(0));
        });
    }
}
