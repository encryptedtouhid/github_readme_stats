using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Interfaces;

namespace GitHubStats.Application.Services;

/// <summary>
/// Service for generating gist cards with caching support.
/// </summary>
public sealed class GistCardService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly ICacheService _cacheService;
    private readonly ICardRenderer _cardRenderer;

    public GistCardService(
        IGitHubClient gitHubClient,
        ICacheService cacheService,
        ICardRenderer cardRenderer)
    {
        _gitHubClient = gitHubClient;
        _cacheService = cacheService;
        _cardRenderer = cardRenderer;
    }

    /// <summary>
    /// Generates a gist card SVG.
    /// </summary>
    public async Task<string> GenerateCardAsync(
        string gistId,
        GistCardOptions options,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"gist:{gistId}";

        var gist = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => await _gitHubClient.GetGistAsync(gistId, ct),
            cacheDuration ?? TimeSpan.FromDays(2),
            cancellationToken);

        return _cardRenderer.RenderGistCard(gist, options);
    }

    /// <summary>
    /// Gets gist data from cache or API.
    /// </summary>
    public async Task<Gist> GetGistAsync(
        string gistId,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"gist:{gistId}";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => await _gitHubClient.GetGistAsync(gistId, ct),
            cacheDuration ?? TimeSpan.FromDays(2),
            cancellationToken);
    }
}
