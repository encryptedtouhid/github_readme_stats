namespace GitHubStats.Domain.Entities;

/// <summary>
/// Represents GitHub user statistics.
/// </summary>
public sealed record UserStats
{
    public required string Name { get; init; }
    public required string Login { get; init; }
    public int TotalStars { get; init; }
    public int TotalCommits { get; init; }
    public int TotalPRs { get; init; }
    public int TotalPRsMerged { get; init; }
    public double MergedPRsPercentage { get; init; }
    public int TotalReviews { get; init; }
    public int TotalIssues { get; init; }
    public int TotalDiscussionsStarted { get; init; }
    public int TotalDiscussionsAnswered { get; init; }
    public int ContributedTo { get; init; }
    public int TotalFollowers { get; init; }
    public int TotalRepos { get; init; }
    public required UserRank Rank { get; init; }
}

/// <summary>
/// Represents user rank based on GitHub activity.
/// </summary>
public sealed record UserRank
{
    public required string Level { get; init; }
    public double Percentile { get; init; }
}
