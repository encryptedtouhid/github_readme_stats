using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Interfaces;

namespace GitHubStats.Application.Services;

/// <summary>
/// Service for generating repository pin cards with caching support.
/// </summary>
public sealed class RepoCardService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly ICacheService _cacheService;
    private readonly ICardRenderer _cardRenderer;

    public RepoCardService(
        IGitHubClient gitHubClient,
        ICacheService cacheService,
        ICardRenderer cardRenderer)
    {
        _gitHubClient = gitHubClient;
        _cacheService = cacheService;
        _cardRenderer = cardRenderer;
    }

    /// <summary>
    /// Generates a repository card SVG.
    /// </summary>
    public async Task<string> GenerateCardAsync(
        string username,
        string repoName,
        RepoCardOptions options,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"repo:{username}:{repoName}";

        var repo = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => await _gitHubClient.GetRepositoryAsync(username, repoName, ct),
            cacheDuration ?? TimeSpan.FromDays(10),
            cancellationToken);

        return _cardRenderer.RenderRepoCard(repo, options);
    }

    /// <summary>
    /// Gets repository data from cache or API.
    /// </summary>
    public async Task<Repository> GetRepositoryAsync(
        string username,
        string repoName,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"repo:{username}:{repoName}";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => await _gitHubClient.GetRepositoryAsync(username, repoName, ct),
            cacheDuration ?? TimeSpan.FromDays(10),
            cancellationToken);
    }
}
