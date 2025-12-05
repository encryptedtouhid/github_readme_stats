using GitHubStats.Domain.Entities;

namespace GitHubStats.Domain.Interfaces;

/// <summary>
/// Interface for GitHub API client operations.
/// </summary>
public interface IGitHubClient
{
    /// <summary>
    /// Fetches user statistics from GitHub.
    /// </summary>
    Task<UserStats> GetUserStatsAsync(
        string username,
        bool includeAllCommits = false,
        IReadOnlyList<string>? excludeRepos = null,
        bool includeMergedPRs = false,
        bool includeDiscussions = false,
        bool includeDiscussionsAnswers = false,
        int? commitsYear = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches repository information from GitHub.
    /// </summary>
    Task<Repository> GetRepositoryAsync(
        string username,
        string repoName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches top languages for a user from GitHub.
    /// </summary>
    Task<TopLanguages> GetTopLanguagesAsync(
        string username,
        IReadOnlyList<string>? excludeRepos = null,
        double sizeWeight = 1,
        double countWeight = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches gist information from GitHub.
    /// </summary>
    Task<Gist> GetGistAsync(
        string gistId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches contribution streak statistics from GitHub.
    /// </summary>
    Task<StreakStats> GetStreakStatsAsync(
        string username,
        int? startingYear = null,
        CancellationToken cancellationToken = default);
}
