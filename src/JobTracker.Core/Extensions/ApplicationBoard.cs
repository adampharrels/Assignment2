using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Extensions;

/// <summary>One column of the Kanban board: a status and the applications in it.</summary>
/// <param name="Status">The status this column shows.</param>
/// <param name="Cards">The applications in this status, newest first.</param>
public record BoardColumn(ApplicationStatus Status, IReadOnlyList<JobApplication> Cards)
{
    /// <summary>The number of cards, shown in the column header.</summary>
    public int Count => Cards.Count;

    /// <summary>True for Rejected, Withdrawn and Ghosted, which the board shows after the pipeline.</summary>
    public bool IsExit => !Status.IsPipeline();
}

/// <summary>Groups applications into the columns of the Kanban board (S3, Sarah).</summary>
public static class ApplicationBoard
{
    /// <summary>
    /// One column per status in enum order: the pipeline from Wishlist to Accepted, then the exits.
    /// Every status gets a column, even when it is empty, so cards can always be dropped there.
    /// </summary>
    /// <param name="applications">The applications to place on the board.</param>
    public static IReadOnlyList<BoardColumn> BuildColumns(IEnumerable<JobApplication> applications)
    {
        ArgumentNullException.ThrowIfNull(applications);

        ILookup<ApplicationStatus, JobApplication> byStatus = applications.ToLookup(application => application.Status);

        return Enum.GetValues<ApplicationStatus>()
            .Select(status => new BoardColumn(
                status,
                byStatus[status]
                    .OrderByDescending(application => application.DateApplied ?? DateTime.MaxValue)
                    .ThenByDescending(application => application.Id)
                    .ToList()))
            .ToList();
    }
}
