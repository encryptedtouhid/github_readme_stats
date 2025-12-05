using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Interfaces;

namespace GitHubStats.Application.Services;

/// <summary>
/// Service for generating streak cards with caching support.
/// </summary>
public sealed class StreakCardService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly ICacheService _cacheService;
    private readonly ICardRenderer _cardRenderer;

    public StreakCardService(
        IGitHubClient gitHubClient,
        ICacheService cacheService,
        ICardRenderer cardRenderer)
    {
        _gitHubClient = gitHubClient;
        _cacheService = cacheService;
        _cardRenderer = cardRenderer;
    }

    /// <summary>
    /// Generates a streak card SVG for a user.
    /// </summary>
    public async Task<string> GenerateCardAsync(
        string username,
        StreakCardOptions options,
        int? startingYear = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(username, startingYear);

        var stats = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => await _gitHubClient.GetStreakStatsAsync(username, startingYear, ct),
            cacheDuration ?? TimeSpan.FromHours(3), // 3-hour cache like original
            cancellationToken);

        return _cardRenderer.RenderStreakCard(stats, options);
    }

    /// <summary>
    /// Gets streak stats from cache or API.
    /// </summary>
    public async Task<StreakStats> GetStatsAsync(
        string username,
        int? startingYear = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(username, startingYear);

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => await _gitHubClient.GetStreakStatsAsync(username, startingYear, ct),
            cacheDuration ?? TimeSpan.FromHours(3),
            cancellationToken);
    }

    private static string GenerateCacheKey(string username, int? startingYear)
    {
        return $"streak:{username}:{startingYear}";
    }
}
