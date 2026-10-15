using JobTracker.Core.Analytics;

namespace JobTracker.Analytics.Statistics;

/// <summary>General-purpose statistics used by every breakdown on the analytics page.</summary>
public static class RateStatistics
{
    /// <summary>z-value for a 95% confidence interval.</summary>
    public const double Z95 = 1.96;

    /// <summary>
    /// Wilson score interval for a proportion (successes out of total).
    /// Unlike the simple "p plus or minus 2 standard errors" interval, it stays inside 0-1 and behaves
    /// sensibly for small groups, which is exactly our situation (5-20 applications per group).
    /// </summary>
    /// <returns>(0, 0) when total is 0.</returns>
    public static (double Low, double High) WilsonInterval(int successes, int total, double z = Z95)
    {
        if (total <= 0)
        {
            return (0, 0);
        }

        if (successes < 0 || successes > total)
        {
            throw new ArgumentOutOfRangeException(nameof(successes), "Successes must be between 0 and total.");
        }

        double n = total;
        double p = successes / n;
        double zSquared = z * z;
        double denominator = 1 + zSquared / n;
        double centre = (p + zSquared / (2 * n)) / denominator;
        double margin = z * Math.Sqrt(p * (1 - p) / n + zSquared / (4 * n * n)) / denominator;

        return (Math.Max(0, centre - margin), Math.Min(1, centre + margin));
    }

    /// <summary>
    /// Groups items and works out each group's success rate, confidence interval and lift.
    /// </summary>
    /// <typeparam name="T">The kind of item, e.g. ApplicationRecord.</typeparam>
    /// <param name="items">The items to group.</param>
    /// <param name="groupSelector">
    /// Returns the groups an item belongs to. An item can be in several groups
    /// (an application asking for React and SQL is counted in both).
    /// </param>
    /// <param name="isSuccess">Decides whether an item counts as a success.</param>
    /// <returns>One GroupRate per group: reliable groups first, then highest rate first.</returns>
    public static List<GroupRate> GroupRates<T>(
        IEnumerable<T> items,
        Func<T, IEnumerable<string>> groupSelector,
        Func<T, bool> isSuccess)
    {
        List<T> itemList = items.ToList();
        if (itemList.Count == 0)
        {
            return new List<GroupRate>();
        }

        double baselineRate = itemList.Count(isSuccess) / (double)itemList.Count;

        return itemList
            .SelectMany(item => groupSelector(item).Distinct().Select(group => new { Group = group, Success = isSuccess(item) }))
            .GroupBy(entry => entry.Group)
            .Select(group =>
            {
                int total = group.Count();
                int successes = group.Count(entry => entry.Success);
                double rate = successes / (double)total;
                (double low, double high) = WilsonInterval(successes, total);
                return new GroupRate
                {
                    Group = group.Key,
                    Total = total,
                    Successes = successes,
                    Rate = rate,
                    CiLow = low,
                    CiHigh = high,
                    Lift = baselineRate > 0 ? rate / baselineRate : 0
                };
            })
            .OrderByDescending(rate => rate.IsReliable)
            .ThenByDescending(rate => rate.Rate)
            .ThenBy(rate => rate.Group)
            .ToList();
    }

    /// <summary>Single-group convenience overload: each item belongs to exactly one group.</summary>
    public static List<GroupRate> GroupRates<T>(IEnumerable<T> items, Func<T, string> groupSelector, Func<T, bool> isSuccess)
    {
        return GroupRates(items, item => new[] { groupSelector(item) }, isSuccess);
    }

    /// <summary>The median of the values, or null for an empty list.</summary>
    public static double? Median(IEnumerable<double> values)
    {
        List<double> sorted = values.OrderBy(value => value).ToList();
        if (sorted.Count == 0)
        {
            return null;
        }

        int middle = sorted.Count / 2;
        if (sorted.Count % 2 == 1)
        {
            return sorted[middle];
        }

        return (sorted[middle - 1] + sorted[middle]) / 2;
    }
}
