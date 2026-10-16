using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Entities;
using JobTracker.Data;
using JobTracker.Data.Services;

namespace JobTracker.Tests.K1;

[TestFixture]
public class ProfileServiceTests
{
    private TestDatabase _database = null!;
    private ProfileService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(_database);
        _service = new ProfileService(_database);
    }

    [TearDown]
    public void TearDown()
    {
        _database.Dispose();
    }

    [Test]
    public async Task InitializeAsync_SeedsProfileAndSkills()
    {
        UserProfile profile = await _service.GetProfileAsync();
        IReadOnlyList<Skill> skills = await _service.GetSkillsAsync();

        Assert.That(profile.ResumeText, Is.Not.Empty);
        Assert.That(skills, Is.Not.Empty);
        Assert.That(skills.Select(skill => skill.Name), Is.Ordered);
    }

    [Test]
    public async Task SaveProfileAsync_UpdatesTheSingleProfile()
    {
        UserProfile profile = await _service.GetProfileAsync();
        profile.FullName = "Jess Nguyen";

        await _service.SaveProfileAsync(profile);
        UserProfile reloaded = await _service.GetProfileAsync();

        Assert.That(reloaded.FullName, Is.EqualTo("Jess Nguyen"));
        await using JobTrackerDbContext context = _database.CreateDbContext();
        Assert.That(context.UserProfiles.Count(), Is.EqualTo(1));
    }

    [Test]
    public void SaveProfileAsync_NameTooLong_ThrowsValidationException()
    {
        var profile = new UserProfile { FullName = new string('x', 101) };

        Assert.ThrowsAsync<ValidationException>(() => _service.SaveProfileAsync(profile));
    }

    [Test]
    public async Task SetInProfileAsync_TogglesSkill()
    {
        Skill java = (await _service.GetSkillsAsync()).First(skill => skill.Name == "java");

        await _service.SetInProfileAsync(java.Id, true);

        Assert.That(await _service.GetProfileSkillNamesAsync(), Does.Contain("java"));
    }

    [Test]
    public void SetInProfileAsync_UnknownSkill_ThrowsKeyNotFound()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.SetInProfileAsync(-1, true));
    }
}
