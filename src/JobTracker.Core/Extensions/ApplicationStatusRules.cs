using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Extensions;

/// <summary>
/// The agreed rules for moving an application between statuses. Used by the application service
/// before saving a move, and by the board to show which columns a card can be dropped into.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Pipeline moves go forward only; skipping stages is allowed (Applied → Interview).</item>
/// <item>Accepted can only be reached from Offer.</item>
/// <item>Any open application can exit to Rejected, Withdrawn or Ghosted, except that a wishlist
/// item was never sent, so it can only be withdrawn.</item>
/// <item>Ghosted can be revived to a pipeline stage later than the furthest one reached, or exit.</item>
/// <item>Accepted, Rejected and Withdrawn are terminal.</item>
/// <item>To fix a mistake, an application can move back to any status it had earlier. Moving back undoes
/// the later moves: they are removed from the history, so analytics never count a stage that was a mistake.</item>
/// </list>
/// </remarks>
public static class ApplicationStatusRules
{
    /// <summary>True for Accepted, Rejected and Withdrawn, which can't be left.</summary>
    public static bool IsTerminal(this ApplicationStatus status) =>
        status is ApplicationStatus.Accepted or ApplicationStatus.Rejected or ApplicationStatus.Withdrawn;

    /// <summary>True for the in-order stages Wishlist to Accepted; false for the exits.</summary>
    public static bool IsPipeline(this ApplicationStatus status) =>
        status is >= ApplicationStatus.Wishlist and <= ApplicationStatus.Accepted;

    /// <summary>Explains why a move is not allowed, or returns null when it is.</summary>
    /// <param name="from">The current status.</param>
    /// <param name="to">The status to move to.</param>
    /// <param name="furthestPipelineStatus">
    /// The furthest pipeline status in the history. Only matters when <paramref name="from"/> is Ghosted;
    /// use <see cref="FurthestPipelineStatus"/> to work it out.
    /// </param>
    /// <returns>A message to show the user, or null.</returns>
    public static string? GetMoveError(ApplicationStatus from, ApplicationStatus to, ApplicationStatus furthestPipelineStatus)
    {
        if (!Enum.IsDefined(from) || !Enum.IsDefined(to) || !Enum.IsDefined(furthestPipelineStatus))
        {
            return "Choose a valid application status.";
        }

        if (from == to)
        {
            return $"The application is already {from.ToDisplayText()}.";
        }

        if (from.IsTerminal())
        {
            return $"{from.ToDisplayText()} is final, so the application can't be moved.";
        }

        if (to.IsPipeline())
        {
            return GetPipelineMoveError(from, to, furthestPipelineStatus);
        }

        // The remaining targets are the exits: Rejected, Withdrawn and Ghosted.
        if (from == ApplicationStatus.Wishlist && to != ApplicationStatus.Withdrawn)
        {
            return $"A wishlist item hasn't been sent yet, so it can't be {to.ToDisplayText()}. Withdraw it instead.";
        }

        return null;
    }

    /// <summary>
    /// Explains why a move is not allowed, or returns null when it is, for an application that is not
    /// Ghosted (its furthest stage is then its current one).
    /// </summary>
    public static string? GetMoveError(ApplicationStatus from, ApplicationStatus to) =>
        GetMoveError(from, to, from.IsPipeline() ? from : ApplicationStatus.Applied);

    /// <summary>True when the move is allowed.</summary>
    public static bool CanMove(ApplicationStatus from, ApplicationStatus to, ApplicationStatus furthestPipelineStatus) =>
        GetMoveError(from, to, furthestPipelineStatus) is null;

    /// <summary>True when the move is allowed, for an application that is not Ghosted.</summary>
    public static bool CanMove(ApplicationStatus from, ApplicationStatus to) => GetMoveError(from, to) is null;

    /// <summary>Throws when the move is not allowed. The application service calls this before saving.</summary>
    /// <exception cref="InvalidOperationException">The move breaks a status rule; the message says which.</exception>
    public static void EnsureCanMove(ApplicationStatus from, ApplicationStatus to, ApplicationStatus furthestPipelineStatus)
    {
        string? error = GetMoveError(from, to, furthestPipelineStatus);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }
    }

    /// <summary>Every status the application could move to next, in enum order.</summary>
    public static IReadOnlyList<ApplicationStatus> AllowedTargets(ApplicationStatus from, ApplicationStatus furthestPipelineStatus) =>
        Enum.GetValues<ApplicationStatus>()
            .Where(to => CanMove(from, to, furthestPipelineStatus))
            .ToList();

    /// <summary>
    /// The furthest pipeline status in a history, ignoring exits. Wishlist when the history is empty.
    /// </summary>
    /// <param name="history">The application's stage events, in any order.</param>
    public static ApplicationStatus FurthestPipelineStatus(this IEnumerable<StageEvent> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        return history
            .Select(stageEvent => stageEvent.ToStatus)
            .Where(status => status.IsPipeline())
            .DefaultIfEmpty(ApplicationStatus.Wishlist)
            .Max();
    }

    /// <summary>
    /// The statuses an application had before its current one, most recent first. Moving back to one of
    /// them undoes every move made after it.
    /// </summary>
    /// <param name="history">The application's stage events, in any order.</param>
    public static IReadOnlyList<ApplicationStatus> MoveBackTargets(this IEnumerable<StageEvent> history)
    {
        List<StageEvent> ordered = InOrder(history);
        if (ordered.Count == 0)
        {
            return Array.Empty<ApplicationStatus>();
        }

        ApplicationStatus current = ordered[^1].ToStatus;
        return ordered
            .Take(ordered.Count - 1)
            .Select(stageEvent => stageEvent.ToStatus)
            .Reverse()
            .Where(status => status != current)
            .Distinct()
            .ToList();
    }

    /// <summary>Explains why the application can't move back to a status, or returns null when it can.</summary>
    /// <param name="history">The application's stage events, in any order.</param>
    /// <param name="earlierStatus">The status to move back to.</param>
    public static string? GetMoveBackError(IEnumerable<StageEvent> history, ApplicationStatus earlierStatus)
    {
        List<StageEvent> ordered = InOrder(history);
        if (ordered.Count > 0 && ordered[^1].ToStatus == earlierStatus)
        {
            return $"The application is already {earlierStatus.ToDisplayText()}.";
        }

        return ordered.MoveBackTargets().Contains(earlierStatus)
            ? null
            : $"The application was never {earlierStatus.ToDisplayText()}, so it can't move back there.";
    }

    /// <summary>
    /// The stage events that moving back to <paramref name="earlierStatus"/> undoes: every event after the
    /// most recent one that reached that status.
    /// </summary>
    /// <param name="history">The application's stage events, in any order.</param>
    /// <param name="earlierStatus">The status to move back to.</param>
    /// <exception cref="InvalidOperationException">The application never had that status before.</exception>
    public static IReadOnlyList<StageEvent> EventsUndoneByMovingBack(this IEnumerable<StageEvent> history, ApplicationStatus earlierStatus)
    {
        string? error = GetMoveBackError(history, earlierStatus);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        List<StageEvent> ordered = InOrder(history);
        int keepUpTo = ordered.FindLastIndex(ordered.Count - 2, stageEvent => stageEvent.ToStatus == earlierStatus);
        return ordered.Skip(keepUpTo + 1).ToList();
    }

    /// <summary>Oldest first; events saved at the same moment stay in the order they were saved.</summary>
    private static List<StageEvent> InOrder(IEnumerable<StageEvent> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        return history.OrderBy(stageEvent => stageEvent.ChangedAt).ThenBy(stageEvent => stageEvent.Id).ToList();
    }

    /// <summary>Checks a move whose target is a pipeline stage.</summary>
    private static string? GetPipelineMoveError(ApplicationStatus from, ApplicationStatus to, ApplicationStatus furthestPipelineStatus)
    {
        if (to == ApplicationStatus.Accepted && from != ApplicationStatus.Offer)
        {
            return "Only an offer can be accepted.";
        }

        if (from == ApplicationStatus.Ghosted)
        {
            return to > furthestPipelineStatus
                ? null
                : $"A ghosted application can only come back at a later stage than {furthestPipelineStatus.ToDisplayText()}.";
        }

        return to > from
            ? null
            : $"Applications only move forward, so it can't go back from {from.ToDisplayText()} to {to.ToDisplayText()}.";
    }
}
