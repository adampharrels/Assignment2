using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Extensions;
using JobTracker.Data;
using JobTracker.Data.Services;
using JobTracker.Tests.K1;

namespace JobTracker.Tests.S3;

[TestFixture]
public class BoardTests
{
    private TestDatabase _database = null!;
    private ApplicationService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = new TestDatabase();
        await DatabaseInitializer.InitializeAsync(_database);
        _service = new ApplicationService(_database);
    }

    [TearDown]
    public void TearDown()
    {
        _database.Dispose();
    }

    [Test]
    public void BuildColumns_HasOneColumnPerStatusInPipelineOrder_EvenWhenEmpty()
    {
        IReadOnlyList<BoardColumn> columns = ApplicationBoard.BuildColumns(Array.Empty<JobApplication>());

        Assert.Multiple(() =>
        {
            Assert.That(columns.Select(column => column.Status), Is.EqualTo(Enum.GetValues<ApplicationStatus>()));
            Assert.That(columns.All(column => column.Count == 0), Is.True);
            Assert.That(columns.Where(column => column.IsExit).Select(column => column.Status),
                Is.EqualTo(new[] { ApplicationStatus.Rejected, ApplicationStatus.Withdrawn, ApplicationStatus.Ghosted }));
        });
    }

    [Test]
    public void BuildColumns_CountsCardsAndPutsNewestFirst()
    {
        var applications = new[]
        {
            new JobApplication { Id = 1, Status = ApplicationStatus.Applied, DateApplied = new DateTime(2026, 9, 1) },
            new JobApplication { Id = 2, Status = ApplicationStatus.Applied, DateApplied = new DateTime(2026, 9, 20) },
            new JobApplication { Id = 3, Status = ApplicationStatus.Interview, DateApplied = new DateTime(2026, 9, 5) }
        };

        IReadOnlyList<BoardColumn> columns = ApplicationBoard.BuildColumns(applications);
        BoardColumn applied = columns.Single(column => column.Status == ApplicationStatus.Applied);

        Assert.Multiple(() =>
        {
            Assert.That(applied.Count, Is.EqualTo(2));
            Assert.That(applied.Cards.Select(card => card.Id), Is.EqualTo(new[] { 2, 1 }));
            Assert.That(columns.Single(column => column.Status == ApplicationStatus.Interview).Count, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DroppingOnALaterStage_SkippingStages_MovesTheCard()
    {
        JobApplication card = await _service.CreateAsync(NewApplication(), "Canva");

        await _service.ChangeStatusAsync(card.Id, ApplicationStatus.FinalRound);

        Assert.That((await _service.GetByIdAsync(card.Id))!.Status, Is.EqualTo(ApplicationStatus.FinalRound));
    }

    [Test]
    public async Task DroppingOnAnEarlierStage_IsRefusedWithAReason()
    {
        JobApplication card = await _service.CreateAsync(NewApplication(), "Canva");
        await _service.ChangeStatusAsync(card.Id, ApplicationStatus.Interview);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => _service.ChangeStatusAsync(card.Id, ApplicationStatus.Assessment));

        Assert.That(exception!.Message, Does.Contain("only move forward"));
        Assert.That((await _service.GetByIdAsync(card.Id))!.Status, Is.EqualTo(ApplicationStatus.Interview));
    }

    [Test]
    public async Task GhostedCard_CanComeBackLaterThanItGot_ButNotEarlier()
    {
        JobApplication card = await _service.CreateAsync(NewApplication(), "Canva");
        await _service.ChangeStatusAsync(card.Id, ApplicationStatus.Assessment);
        await _service.ChangeStatusAsync(card.Id, ApplicationStatus.Ghosted);

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.ChangeStatusAsync(card.Id, ApplicationStatus.Assessment));
        await _service.ChangeStatusAsync(card.Id, ApplicationStatus.Interview);

        Assert.That((await _service.GetByIdAsync(card.Id))!.Status, Is.EqualTo(ApplicationStatus.Interview));
    }

    [TestCase(ApplicationStatus.Rejected)]
    [TestCase(ApplicationStatus.Withdrawn)]
    public async Task RightClickRejectOrWithdraw_EndsTheApplication(ApplicationStatus exit)
    {
        JobApplication card = await _service.CreateAsync(NewApplication(), "Canva");

        await _service.ChangeStatusAsync(card.Id, exit);

        JobApplication reloaded = (await _service.GetByIdAsync(card.Id))!;
        Assert.Multiple(() =>
        {
            Assert.That(reloaded.Status, Is.EqualTo(exit));
            Assert.That(ApplicationStatusRules.CanMove(reloaded.Status, ApplicationStatus.Interview), Is.False);
        });
    }

    /// <summary>A valid, submitted application.</summary>
    private static JobApplication NewApplication() => new JobApplication
    {
        RoleTitle = "Graduate Software Engineer",
        Location = "Sydney",
        Status = ApplicationStatus.Applied,
        DateApplied = new DateTime(2026, 9, 1)
    };
}
