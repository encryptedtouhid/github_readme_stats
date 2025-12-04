using System.ComponentModel.DataAnnotations;

namespace GitHubStats.Infrastructure.Configuration;

/// <summary>
/// Configuration options for GitHub API access.
/// </summary>
public sealed class GitHubOptions
{
    public const string SectionName = "GitHub";

    /// <summary>
    /// List of Personal Access Tokens for GitHub API.
    /// Multiple tokens enable round-robin and fallback on rate limits.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> PersonalAccessTokens { get; set; } = [];

    /// <summary>
    /// Whether to fetch all stars across multiple pages.
    /// Disabled by default to reduce API calls.
    /// </summary>
    public bool FetchMultiPageStars { get; set; } = false;

    /// <summary>
    /// GitHub GraphQL API endpoint.
    /// </summary>
    public string GraphQLEndpoint { get; set; } = "https://api.github.com/graphql";

    /// <summary>
    /// GitHub REST API endpoint.
    /// </summary>
    public string RestApiEndpoint { get; set; } = "https://api.github.com";

    /// <summary>
    /// Maximum retry attempts when rate limited.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Timeout for HTTP requests in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
