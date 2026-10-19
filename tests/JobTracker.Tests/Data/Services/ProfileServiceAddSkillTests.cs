using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Data;
using JobTracker.Data.Services;

namespace JobTracker.Tests.Data.Services;

/// <summary>G6 (Grace): adding skills from the profile page. ProfileServiceTests (K1, Adam) covers the rest.</summary>
[TestFixture]
public class ProfileServiceAddSkillTests
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
    public async Task AddSkillAsync_NewSkill_IsSavedInLowerCaseAndTicked()
    {
        Skill added = await _service.AddSkillAsync("  GraphQL ", SkillCategory.Framework);

        IReadOnlyList<Skill> skills = await _service.GetSkillsAsync();
        Skill saved = skills.Single(skill => skill.Id == added.Id);
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(saved.Name, Is.EqualTo("graphql"));
            Assert.That(saved.Category, Is.EqualTo(SkillCategory.Framework));
            Assert.That(saved.InMyProfile, Is.True);
            Assert.That(await _service.GetProfileSkillNamesAsync(), Does.Contain("graphql"));
        });
    }

    [TestCase("React")]
    [TestCase("  react  ")]
    public void AddSkillAsync_NameAlreadyInTheList_Throws(string name)
    {
        var exception = Assert.ThrowsAsync<ValidationException>(() => _service.AddSkillAsync(name, SkillCategory.Framework));

        Assert.That(exception!.Message, Does.Contain("already in your skill list"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void AddSkillAsync_BlankName_Throws(string name)
    {
        var exception = Assert.ThrowsAsync<ValidationException>(() => _service.AddSkillAsync(name, SkillCategory.Other));

        Assert.That(exception!.Message, Is.EqualTo("Enter a skill name."));
    }

    [Test]
    public async Task AddSkillAsync_NameTooLong_ThrowsAndSavesNothing()
    {
        int before = (await _service.GetSkillsAsync()).Count;

        Assert.ThrowsAsync<ValidationException>(() => _service.AddSkillAsync(new string('x', 61), SkillCategory.Other));
        Assert.That(await _service.GetSkillsAsync(), Has.Count.EqualTo(before));
    }
}
