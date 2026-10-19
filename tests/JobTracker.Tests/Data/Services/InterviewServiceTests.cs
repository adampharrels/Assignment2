using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Analytics;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Interfaces;
using JobTracker.Data.Services;

namespace JobTracker.Tests.Data.Services;

[TestFixture]
public class InterviewServiceTests
{
    private TestDatabase _database = null!;
    private InterviewService _service = null!;
    private static readonly DateTime Start = new(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);

    [SetUp]
    public async Task SetUp()
    {
        _database = new TestDatabase();
        await using var db = _database.CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        _service = new InterviewService(_database, new ApplicationSource());
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    [TestCase(-30, 60)]
    [TestCase(30, 60)]
    [TestCase(0, 60)]
    [TestCase(-30, 120)]
    [TestCase(10, 10)]
    public async Task Create_RejectsEveryOverlapShapeAcrossApplications(int offset, int duration)
    {
        await _service.CreateAsync(NewInterview());
        var overlapping = NewInterview(offset, duration);
        overlapping.ApplicationId = 2;
        Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(overlapping));
        Assert.That((await _service.GetByApplicationAsync(2)), Is.Empty);
    }

    [Test]
    public async Task AdjacentAndCancelledInterviews_AreAllowed_ButRestorationIsChecked()
    {
        await _service.CreateAsync(NewInterview());
        await _service.CreateAsync(NewInterview(60));
        await _service.CreateAsync(NewInterview(-60));
        var cancelled = NewInterview(15);
        cancelled.Outcome = InterviewOutcome.Cancelled;
        cancelled = await _service.CreateAsync(cancelled);
        Assert.ThrowsAsync<InvalidOperationException>(() => _service.SetOutcomeAsync(cancelled.Id, InterviewOutcome.Pending));
        Assert.That((await _service.GetByIdAsync(cancelled.Id))!.Outcome, Is.EqualTo(InterviewOutcome.Cancelled));
    }

    [Test]
    public async Task Update_ExcludesItself_AndRejectsOverlapWithoutSaving()
    {
        var first = await _service.CreateAsync(NewInterview());
        first.PrepNotes = "Prepare examples";
        await _service.UpdateAsync(first);
        var second = await _service.CreateAsync(NewInterview(60));
        second.ScheduledAt = Start.AddMinutes(30);
        Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(second));
        Assert.That((await _service.GetByIdAsync(second.Id))!.ScheduledAt, Is.EqualTo(Start.AddMinutes(60)));
        Assert.That((await _service.GetByIdAsync(first.Id))!.PrepNotes, Is.EqualTo("Prepare examples"));
    }

    [Test]
    public async Task Range_IsStartInclusiveEndExclusive_OrderedAndUtc_IncludesCancelled()
    {
        await _service.CreateAsync(NewInterview(120));
        var cancelled = NewInterview(60);
        cancelled.Outcome = InterviewOutcome.Cancelled;
        await _service.CreateAsync(cancelled);
        await _service.CreateAsync(NewInterview());
        var results = await _service.GetByDateRangeAsync(Start, Start.AddHours(2));
        Assert.That(results.Select(item => item.ScheduledAt), Is.EqualTo(new[] { Start, Start.AddHours(1) }));
        Assert.That(results.All(item => item.ScheduledAt.Kind == DateTimeKind.Utc), Is.True);
        Assert.That(results.Last().Outcome, Is.EqualTo(InterviewOutcome.Cancelled));
        Assert.That(await _service.GetByApplicationAsync(99), Is.Empty);
    }

    [Test]
    public async Task OutcomesAndDelete_ArePersisted_MissingMutationsAreRejected()
    {
        var item = await _service.CreateAsync(NewInterview());
        foreach (var outcome in new[] { InterviewOutcome.Passed, InterviewOutcome.Failed, InterviewOutcome.Cancelled })
        {
            await _service.SetOutcomeAsync(item.Id, outcome);
            Assert.That((await _service.GetByIdAsync(item.Id))!.Outcome, Is.EqualTo(outcome));
        }
        await _service.DeleteAsync(item.Id);
        Assert.That(await _service.GetByIdAsync(item.Id), Is.Null);
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(item.Id));
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(item));
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.SetOutcomeAsync(item.Id, InterviewOutcome.Passed));
    }

    [Test]
    public void InvalidFieldsAndRanges_AreRejected()
    {
        var invalid = new[]
        {
            new Interview { ApplicationId = 99, ScheduledAt = Start },
            new Interview { ApplicationId = 1, ScheduledAt = default },
            new Interview { ApplicationId = 1, ScheduledAt = DateTime.SpecifyKind(Start, DateTimeKind.Unspecified) },
            NewInterview(0, 4), NewInterview(0, 601),
            new Interview { ApplicationId = 1, ScheduledAt = Start, Type = (InterviewType)99 },
            new Interview { ApplicationId = 1, ScheduledAt = Start, Outcome = (InterviewOutcome)99 },
            new Interview { ApplicationId = 1, ScheduledAt = Start, Id = 99 }
        };
        foreach (var item in invalid)
            Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(item));
        Assert.ThrowsAsync<ValidationException>(() => _service.GetByDateRangeAsync(Start, Start));
        Assert.ThrowsAsync<ValidationException>(() => _service.GetByDateRangeAsync(DateTime.SpecifyKind(Start, DateTimeKind.Local), Start.AddDays(1)));
        Assert.ThrowsAsync<ValidationException>(() => _service.SetOutcomeAsync(1, (InterviewOutcome)99));
    }

    [Test]
    public async Task ConcurrentCreates_OnlyOneOverlappingInterviewIsSaved()
    {
        async Task<bool> Create()
        {
            try { await _service.CreateAsync(NewInterview()); return true; }
            catch (InvalidOperationException) { return false; }
        }
        var results = await Task.WhenAll(Create(), Create());
        Assert.That(results.Count(success => success), Is.EqualTo(1));
    }

    private static Interview NewInterview(int offset = 0, int duration = 60) => new()
    { ApplicationId = 1, ScheduledAt = Start.AddMinutes(offset), DurationMinutes = duration, Type = InterviewType.Technical };

    private sealed class ApplicationSource : IApplicationDataSource
    {
        public Task<IReadOnlyList<ApplicationRecord>> GetApplicationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ApplicationRecord>>(new[] { new ApplicationRecord { Id = 1 }, new ApplicationRecord { Id = 2 } });
    }
}
