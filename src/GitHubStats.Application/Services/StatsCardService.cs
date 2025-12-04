using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Interfaces;

namespace GitHubStats.Application.Services;

/// <summary>
/// Service for generating stats cards with caching support.
/// </summary>
public sealed class StatsCardService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly ICacheService _cacheService;
    private readonly ICardRenderer _cardRenderer;

    public StatsCardService(
        IGitHubClient gitHubClient,
        ICacheService cacheService,
        ICardRenderer cardRenderer)
    {
        _gitHubClient = gitHubClient;
        _cacheService = cacheService;
        _cardRenderer = cardRenderer;
    }

    /// <summary>
    /// Generates a stats card SVG for a user.
    /// </summary>
    public async Task<string> GenerateCardAsync(
        string username,
        StatsCardOptions options,
        bool includeAllCommits = false,
        IReadOnlyList<string>? excludeRepos = null,
        bool includeMergedPRs = false,
        bool includeDiscussions = false,
        bool includeDiscussionsAnswers = false,
        int? commitsYear = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(username, includeAllCommits, excludeRepos,
            includeMergedPRs, includeDiscussions, includeDiscussionsAnswers, commitsYear);

        var stats = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => await _gitHubClient.GetUserStatsAsync(
                username, includeAllCommits, excludeRepos,
                includeMergedPRs, includeDiscussions, includeDiscussionsAnswers,
                commitsYear, ct),
            cacheDuration ?? TimeSpan.FromDays(1),
            cancellationToken);

        return _cardRenderer.RenderStatsCard(stats, options);
    }

    /// <summary>
    /// Gets user stats from cache or API.
    /// </summary>
    public async Task<UserStats> GetStatsAsync(
        string username,
        bool includeAllCommits = false,
        IReadOnlyList<string>? excludeRepos = null,
        bool includeMergedPRs = false,
        bool includeDiscussions = false,
        bool includeDiscussionsAnswers = false,
        int? commitsYear = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(username, includeAllCommits, excludeRepos,
            includeMergedPRs, includeDiscussions, includeDiscussionsAnswers, commitsYear);

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => await _gitHubClient.GetUserStatsAsync(
                username, includeAllCommits, excludeRepos,
                includeMergedPRs, includeDiscussions, includeDiscussionsAnswers,
                commitsYear, ct),
            cacheDuration ?? TimeSpan.FromDays(1),
            cancellationToken);
    }

    private static string GenerateCacheKey(
        string username,
        bool includeAllCommits,
        IReadOnlyList<string>? excludeRepos,
        bool includeMergedPRs,
        bool includeDiscussions,
        bool includeDiscussionsAnswers,
        int? commitsYear)
    {
        var excludeHash = excludeRepos?.Count > 0
            ? string.Join(",", excludeRepos.OrderBy(r => r)).GetHashCode()
            : 0;

        return $"stats:{username}:{includeAllCommits}:{excludeHash}:{includeMergedPRs}:{includeDiscussions}:{includeDiscussionsAnswers}:{commitsYear}";
    }
}
