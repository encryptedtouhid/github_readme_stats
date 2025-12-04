namespace GitHubStats.Domain.Entities;

/// <summary>
/// Represents a GitHub Gist.
/// </summary>
public sealed record Gist
{
    public required string Name { get; init; }
    public required string NameWithOwner { get; init; }
    public string? Description { get; init; }
    public string? Language { get; init; }
    public int StarsCount { get; init; }
    public int ForksCount { get; init; }
}
