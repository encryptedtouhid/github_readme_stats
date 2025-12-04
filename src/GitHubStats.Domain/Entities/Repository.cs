namespace GitHubStats.Domain.Entities;

/// <summary>
/// Represents a GitHub repository.
/// </summary>
public sealed record Repository
{
    public required string Name { get; init; }
    public required string NameWithOwner { get; init; }
    public string? Description { get; init; }
    public bool IsPrivate { get; init; }
    public bool IsArchived { get; init; }
    public bool IsTemplate { get; init; }
    public int StarCount { get; init; }
    public int ForkCount { get; init; }
    public PrimaryLanguage? PrimaryLanguage { get; init; }
}

/// <summary>
/// Represents the primary programming language of a repository.
/// </summary>
public sealed record PrimaryLanguage
{
    public required string Name { get; init; }
    public string? Color { get; init; }
}
