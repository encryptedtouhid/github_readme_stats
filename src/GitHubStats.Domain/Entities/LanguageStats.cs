namespace GitHubStats.Domain.Entities;

/// <summary>
/// Represents language usage statistics.
/// </summary>
public sealed record LanguageStats
{
    public required string Name { get; init; }
    public string Color { get; init; } = "#858585";
    public long Size { get; init; }
    public int RepoCount { get; init; }
    public double Percentage { get; init; }
}

/// <summary>
/// Represents a collection of top languages for a user.
/// </summary>
public sealed record TopLanguages
{
    public required IReadOnlyList<LanguageStats> Languages { get; init; }
    public long TotalSize { get; init; }
}
