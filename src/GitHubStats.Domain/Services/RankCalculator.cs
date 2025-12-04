using GitHubStats.Domain.Entities;

namespace GitHubStats.Domain.Services;

/// <summary>
/// Calculates user rank based on GitHub activity metrics.
/// </summary>
public static class RankCalculator
{
    private static readonly string[] Levels = ["S", "A+", "A", "A-", "B+", "B", "B-", "C+", "C"];
    private static readonly double[] Thresholds = [1, 12.5, 25, 37.5, 50, 62.5, 75, 87.5, 100];

    // Median values for normalization
    private const int CommitsMedianDefault = 250;
    private const int CommitsMedianAllTime = 1000;
    private const int PrsMedian = 50;
    private const int IssuesMedian = 25;
    private const int ReviewsMedian = 2;
    private const int StarsMedian = 50;
    private const int FollowersMedian = 10;

    // Weights for each metric
    private const double CommitsWeight = 2;
    private const double PrsWeight = 3;
    private const double IssuesWeight = 1;
    private const double ReviewsWeight = 1;
    private const double StarsWeight = 4;
    private const double FollowersWeight = 1;

    private const double TotalWeight = CommitsWeight + PrsWeight + IssuesWeight + ReviewsWeight + StarsWeight + FollowersWeight;

    /// <summary>
    /// Calculates the user rank based on GitHub activity.
    /// </summary>
    public static UserRank Calculate(
        int commits,
        int prs,
        int issues,
        int reviews,
        int stars,
        int followers,
        bool allCommits = false)
    {
        var commitsMedian = allCommits ? CommitsMedianAllTime : CommitsMedianDefault;

        var rank = 1 - (
            CommitsWeight * ExponentialCdf((double)commits / commitsMedian) +
            PrsWeight * ExponentialCdf((double)prs / PrsMedian) +
            IssuesWeight * ExponentialCdf((double)issues / IssuesMedian) +
            ReviewsWeight * ExponentialCdf((double)reviews / ReviewsMedian) +
            StarsWeight * LogNormalCdf((double)stars / StarsMedian) +
            FollowersWeight * LogNormalCdf((double)followers / FollowersMedian)
        ) / TotalWeight;

        var percentile = rank * 100;
        var level = GetLevel(percentile);

        return new UserRank
        {
            Level = level,
            Percentile = percentile
        };
    }

    private static double ExponentialCdf(double x)
    {
        return 1 - Math.Pow(2, -x);
    }

    private static double LogNormalCdf(double x)
    {
        return x / (1 + x);
    }

    private static string GetLevel(double percentile)
    {
        for (var i = 0; i < Thresholds.Length; i++)
        {
            if (percentile <= Thresholds[i])
            {
                return Levels[i];
            }
        }
        return Levels[^1];
    }
}
