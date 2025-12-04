namespace GitHubStats.Infrastructure.Configuration;

/// <summary>
/// Configuration options for access control.
/// </summary>
public sealed class AccessControlOptions
{
    public const string SectionName = "AccessControl";

    /// <summary>
    /// List of whitelisted usernames. If set, only these users can use the API.
    /// </summary>
    public List<string>? Whitelist { get; set; }

    /// <summary>
    /// List of whitelisted gist IDs.
    /// </summary>
    public List<string>? GistWhitelist { get; set; }

    /// <summary>
    /// List of blacklisted usernames.
    /// </summary>
    public List<string> Blacklist { get; set; } = [];

    /// <summary>
    /// List of repositories to exclude from stats.
    /// </summary>
    public List<string> ExcludeRepositories { get; set; } = [];
}
