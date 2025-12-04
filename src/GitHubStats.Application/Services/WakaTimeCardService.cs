using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Interfaces;

namespace GitHubStats.Application.Services;

/// <summary>
/// Service for generating WakaTime cards with caching support.
/// </summary>
public sealed class WakaTimeCardService
{
    private readonly IWakaTimeClient _wakaTimeClient;
    private readonly ICacheService _cacheService;
    private readonly ICardRenderer _cardRenderer;

    public WakaTimeCardService(
        IWakaTimeClient wakaTimeClient,
        ICacheService cacheService,
        ICardRenderer cardRenderer)
    {
        _wakaTimeClient = wakaTimeClient;
        _cacheService = cacheService;
        _cardRenderer = cardRenderer;
    }

    /// <summary>
    /// Generates a WakaTime card SVG.
    /// </summary>
    public async Task<string> GenerateCardAsync(
        string username,
        WakaTimeCardOptions options,
        string? apiDomain = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"wakatime:{username}:{apiDomain ?? "default"}";

        var stats = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => await _wakaTimeClient.GetStatsAsync(username, apiDomain, ct),
            cacheDuration ?? TimeSpan.FromDays(1),
            cancellationToken);

        return _cardRenderer.RenderWakaTimeCard(stats, options);
    }

    /// <summary>
    /// Gets WakaTime stats from cache or API.
    /// </summary>
    public async Task<WakaTimeStats> GetStatsAsync(
        string username,
        string? apiDomain = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"wakatime:{username}:{apiDomain ?? "default"}";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => await _wakaTimeClient.GetStatsAsync(username, apiDomain, ct),
            cacheDuration ?? TimeSpan.FromDays(1),
            cancellationToken);
    }
}
