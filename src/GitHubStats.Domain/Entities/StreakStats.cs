namespace GitHubStats.Domain.Entities;

/// <summary>
/// Represents GitHub contribution streak statistics.
/// </summary>
public sealed record StreakStats
{
    public required string Username { get; init; }
    public int TotalContributions { get; init; }
    public required StreakInfo CurrentStreak { get; init; }
    public required StreakInfo LongestStreak { get; init; }
    public DateOnly? FirstContribution { get; init; }
}

/// <summary>
/// Represents a streak period.
/// </summary>
public sealed record StreakInfo
{
    public int Length { get; init; }
    public DateOnly? Start { get; init; }
    public DateOnly? End { get; init; }
}
