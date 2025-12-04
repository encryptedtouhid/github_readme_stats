using GitHubStats.Domain.Entities;

namespace GitHubStats.Domain.Interfaces;

/// <summary>
/// Interface for WakaTime API client operations.
/// </summary>
public interface IWakaTimeClient
{
    /// <summary>
    /// Fetches WakaTime statistics for a user.
    /// </summary>
    Task<WakaTimeStats> GetStatsAsync(
        string username,
        string? apiDomain = null,
        CancellationToken cancellationToken = default);
}
