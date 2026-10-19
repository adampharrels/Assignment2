using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Extensions;

namespace JobTracker.Tests.Core.Extensions;

[TestFixture]
public class ApplicationStatusRulesTests
{
    [TestCase(ApplicationStatus.Wishlist, ApplicationStatus.Applied)]
    [TestCase(ApplicationStatus.Applied, ApplicationStatus.Assessment)]
    [TestCase(ApplicationStatus.Applied, ApplicationStatus.Interview)]
    [TestCase(ApplicationStatus.Interview, ApplicationStatus.Offer)]
    [TestCase(ApplicationStatus.Offer, ApplicationStatus.Accepted)]
    public void CanMove_ForwardOrSkippingStages_IsAllowed(ApplicationStatus from, ApplicationStatus to)
    {
        Assert.That(ApplicationStatusRules.CanMove(from, to), Is.True);
    }

    [TestCase(ApplicationStatus.Interview, ApplicationStatus.Applied)]
    [TestCase(ApplicationStatus.Assessment, ApplicationStatus.Wishlist)]
    [TestCase(ApplicationStatus.Offer, ApplicationStatus.Interview)]
    public void CanMove_Backwards_IsNotAllowed(ApplicationStatus from, ApplicationStatus to)
    {
        Assert.That(ApplicationStatusRules.CanMove(from, to), Is.False);
    }

    [TestCase(ApplicationStatus.Applied, ApplicationStatus.Rejected)]
    [TestCase(ApplicationStatus.Interview, ApplicationStatus.Withdrawn)]
    [TestCase(ApplicationStatus.Assessment, ApplicationStatus.Ghosted)]
    [TestCase(ApplicationStatus.Wishlist, ApplicationStatus.Withdrawn)]
    public void CanMove_OpenApplicationToAnExit_IsAllowed(ApplicationStatus from, ApplicationStatus to)
    {
        Assert.That(ApplicationStatusRules.CanMove(from, to), Is.True);
    }

    [TestCase(ApplicationStatus.Accepted)]
    [TestCase(ApplicationStatus.Rejected)]
    [TestCase(ApplicationStatus.Withdrawn)]
    public void AllowedTargets_FromTerminalStatus_IsEmpty(ApplicationStatus terminal)
    {
        Assert.That(ApplicationStatusRules.AllowedTargets(terminal, ApplicationStatus.Offer), Is.Empty);
    }

    [Test]
    public void CanMove_ToAcceptedWithoutAnOffer_IsNotAllowed()
    {
        Assert.That(ApplicationStatusRules.CanMove(ApplicationStatus.Interview, ApplicationStatus.Accepted), Is.False);
    }

    [TestCase(ApplicationStatus.Rejected)]
    [TestCase(ApplicationStatus.Ghosted)]
    public void CanMove_WishlistToRejectedOrGhosted_IsNotAllowed(ApplicationStatus to)
    {
        Assert.That(ApplicationStatusRules.CanMove(ApplicationStatus.Wishlist, to), Is.False);
    }

    [Test]
    public void CanMove_GhostedRevivedLaterThanFurthestStage_IsAllowed()
    {
        bool allowed = ApplicationStatusRules.CanMove(
            ApplicationStatus.Ghosted, ApplicationStatus.Interview, furthestPipelineStatus: ApplicationStatus.Assessment);

        Assert.That(allowed, Is.True);
    }

    [Test]
    public void CanMove_GhostedRevivedAtOrBeforeFurthestStage_IsNotAllowed()
    {
        bool allowed = ApplicationStatusRules.CanMove(
            ApplicationStatus.Ghosted, ApplicationStatus.Assessment, furthestPipelineStatus: ApplicationStatus.Interview);

        Assert.That(allowed, Is.False);
    }

    [Test]
    public void EnsureCanMove_IllegalMove_ThrowsWithAReadableReason()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplicationStatusRules.EnsureCanMove(ApplicationStatus.Rejected, ApplicationStatus.Interview, ApplicationStatus.Interview));

        Assert.That(exception!.Message, Does.Contain("final"));
    }

    [Test]
    public void FurthestPipelineStatus_IgnoresExitsAndOrder()
    {
        var history = new List<StageEvent>
        {
            new StageEvent { ToStatus = ApplicationStatus.Rejected },
            new StageEvent { ToStatus = ApplicationStatus.Applied },
            new StageEvent { ToStatus = ApplicationStatus.Interview },
            new StageEvent { ToStatus = ApplicationStatus.Ghosted }
        };

        Assert.That(history.FurthestPipelineStatus(), Is.EqualTo(ApplicationStatus.Interview));
    }

    [Test]
    public void FurthestPipelineStatus_EmptyHistory_IsWishlist()
    {
        Assert.That(new List<StageEvent>().FurthestPipelineStatus(), Is.EqualTo(ApplicationStatus.Wishlist));
    }

    [Test]
    public void MoveBackTargets_ListsEarlierStatusesMostRecentFirst()
    {
        List<StageEvent> history = History(ApplicationStatus.Applied, ApplicationStatus.Interview, ApplicationStatus.Offer);

        Assert.That(history.MoveBackTargets(), Is.EqualTo(new[] { ApplicationStatus.Interview, ApplicationStatus.Applied }));
    }

    [Test]
    public void MoveBackTargets_FromTerminalStatus_OffersTheStatusBefore()
    {
        List<StageEvent> history = History(ApplicationStatus.Applied, ApplicationStatus.Rejected);

        Assert.That(history.MoveBackTargets(), Is.EqualTo(new[] { ApplicationStatus.Applied }));
    }

    [Test]
    public void EventsUndoneByMovingBack_ReturnsEveryLaterMove()
    {
        List<StageEvent> history = History(ApplicationStatus.Applied, ApplicationStatus.Interview, ApplicationStatus.Offer);

        IReadOnlyList<StageEvent> undone = history.EventsUndoneByMovingBack(ApplicationStatus.Applied);

        Assert.That(undone.Select(stageEvent => stageEvent.ToStatus),
            Is.EqualTo(new[] { ApplicationStatus.Interview, ApplicationStatus.Offer }));
    }

    [Test]
    public void GetMoveBackError_StatusNeverReached_ExplainsWhy()
    {
        List<StageEvent> history = History(ApplicationStatus.Applied, ApplicationStatus.Interview);

        Assert.That(ApplicationStatusRules.GetMoveBackError(history, ApplicationStatus.Assessment), Does.Contain("never"));
        Assert.That(() => history.EventsUndoneByMovingBack(ApplicationStatus.Assessment), Throws.InvalidOperationException);
    }

    /// <summary>A history that visits the statuses in order, one day apart.</summary>
    private static List<StageEvent> History(params ApplicationStatus[] statuses) =>
        statuses.Select((status, index) => new StageEvent
        {
            Id = index + 1,
            FromStatus = index == 0 ? null : statuses[index - 1],
            ToStatus = status,
            ChangedAt = new DateTime(2026, 9, 1).AddDays(index)
        }).ToList();
}
