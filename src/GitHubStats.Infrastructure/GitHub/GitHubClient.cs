using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Exceptions;
using GitHubStats.Domain.Interfaces;
using GitHubStats.Domain.Services;
using GitHubStats.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GitHubStats.Infrastructure.GitHub;

/// <summary>
/// High-performance GitHub API client with resilience patterns.
/// </summary>
public sealed class GitHubClient : IGitHubClient
{
    private readonly HttpClient _httpClient;
    private readonly TokenRotator _tokenRotator;
    private readonly GitHubOptions _options;
    private readonly AccessControlOptions _accessControlOptions;
    private readonly ILogger<GitHubClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GitHubClient(
        HttpClient httpClient,
        TokenRotator tokenRotator,
        IOptions<GitHubOptions> options,
        IOptions<AccessControlOptions> accessControlOptions,
        ILogger<GitHubClient> logger)
    {
        _httpClient = httpClient;
        _tokenRotator = tokenRotator;
        _options = options.Value;
        _accessControlOptions = accessControlOptions.Value;
        _logger = logger;
    }

    public async Task<UserStats> GetUserStatsAsync(
        string username,
        bool includeAllCommits = false,
        IReadOnlyList<string>? excludeRepos = null,
        bool includeMergedPRs = false,
        bool includeDiscussions = false,
        bool includeDiscussionsAnswers = false,
        int? commitsYear = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new MissingParameterException(["username"]);
        }

        var variables = new Dictionary<string, object?>
        {
            ["login"] = username,
            ["after"] = null,
            ["includeMergedPullRequests"] = includeMergedPRs,
            ["includeDiscussions"] = includeDiscussions,
            ["includeDiscussionsAnswers"] = includeDiscussionsAnswers,
            ["startTime"] = commitsYear.HasValue ? $"{commitsYear}-01-01T00:00:00Z" : null
        };

        var response = await ExecuteGraphQLAsync<UserStatsResponse>(
            GraphQLQueries.UserStatsQuery,
            variables,
            cancellationToken);

        if (response.Data?.User == null)
        {
            throw new UserNotFoundException(username);
        }

        var user = response.Data.User;
        var allExcludedRepos = (excludeRepos ?? [])
            .Concat(_accessControlOptions.ExcludeRepositories)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Calculate total stars excluding specified repos
        var totalStars = user.Repositories.Nodes
            .Where(r => !allExcludedRepos.Contains(r.Name))
            .Sum(r => r.Stargazers.TotalCount);

        // Fetch all commits if requested
        int totalCommits;
        if (includeAllCommits)
        {
            totalCommits = await FetchTotalCommitsAsync(username, cancellationToken);
        }
        else
        {
            totalCommits = user.Commits.TotalCommitContributions;
        }

        var totalIssues = user.OpenIssues.TotalCount + user.ClosedIssues.TotalCount;
        var totalPRsMerged = includeMergedPRs ? user.MergedPullRequests?.TotalCount ?? 0 : 0;
        var mergedPRsPercentage = user.PullRequests.TotalCount > 0
            ? (double)totalPRsMerged / user.PullRequests.TotalCount * 100
            : 0;

        var rank = RankCalculator.Calculate(
            totalCommits,
            user.PullRequests.TotalCount,
            totalIssues,
            user.Reviews.TotalPullRequestReviewContributions,
            totalStars,
            user.Followers.TotalCount,
            includeAllCommits);

        return new UserStats
        {
            Name = user.Name ?? user.Login,
            Login = user.Login,
            TotalStars = totalStars,
            TotalCommits = totalCommits,
            TotalPRs = user.PullRequests.TotalCount,
            TotalPRsMerged = totalPRsMerged,
            MergedPRsPercentage = mergedPRsPercentage,
            TotalReviews = user.Reviews.TotalPullRequestReviewContributions,
            TotalIssues = totalIssues,
            TotalDiscussionsStarted = includeDiscussions ? user.RepositoryDiscussions?.TotalCount ?? 0 : 0,
            TotalDiscussionsAnswered = includeDiscussionsAnswers ? user.RepositoryDiscussionComments?.TotalCount ?? 0 : 0,
            ContributedTo = user.RepositoriesContributedTo.TotalCount,
            TotalFollowers = user.Followers.TotalCount,
            TotalRepos = user.Repositories.TotalCount,
            Rank = rank
        };
    }

    public async Task<Repository> GetRepositoryAsync(
        string username,
        string repoName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new MissingParameterException(["username"]);
        }
        if (string.IsNullOrWhiteSpace(repoName))
        {
            throw new MissingParameterException(["repo"]);
        }

        var variables = new Dictionary<string, object?>
        {
            ["login"] = username,
            ["repo"] = repoName
        };

        var response = await ExecuteGraphQLAsync<RepositoryResponse>(
            GraphQLQueries.RepositoryQuery,
            variables,
            cancellationToken);

        var repoData = response.Data?.User?.Repository ?? response.Data?.Organization?.Repository;

        if (repoData == null || repoData.IsPrivate)
        {
            throw new RepositoryNotFoundException(username, repoName);
        }

        return new Repository
        {
            Name = repoData.Name,
            NameWithOwner = repoData.NameWithOwner,
            Description = repoData.Description,
            IsPrivate = repoData.IsPrivate,
            IsArchived = repoData.IsArchived,
            IsTemplate = repoData.IsTemplate,
            StarCount = repoData.Stargazers.TotalCount,
            ForkCount = repoData.ForkCount,
            PrimaryLanguage = repoData.PrimaryLanguage != null
                ? new PrimaryLanguage
                {
                    Name = repoData.PrimaryLanguage.Name,
                    Color = repoData.PrimaryLanguage.Color
                }
                : null
        };
    }

    public async Task<TopLanguages> GetTopLanguagesAsync(
        string username,
        IReadOnlyList<string>? excludeRepos = null,
        double sizeWeight = 1,
        double countWeight = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new MissingParameterException(["username"]);
        }

        var variables = new Dictionary<string, object?>
        {
            ["login"] = username
        };

        var response = await ExecuteGraphQLAsync<TopLanguagesResponse>(
            GraphQLQueries.TopLanguagesQuery,
            variables,
            cancellationToken);

        if (response.Data?.User == null)
        {
            throw new UserNotFoundException(username);
        }

        var allExcludedRepos = (excludeRepos ?? [])
            .Concat(_accessControlOptions.ExcludeRepositories)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Aggregate language data
        var languageData = new Dictionary<string, (string Color, long Size, int Count)>();

        foreach (var repo in response.Data.User.Repositories.Nodes)
        {
            if (allExcludedRepos.Contains(repo.Name))
                continue;

            foreach (var langEdge in repo.Languages.Edges)
            {
                var langName = langEdge.Node.Name;
                var langColor = langEdge.Node.Color ?? "#858585";
                var size = langEdge.Size;

                if (languageData.TryGetValue(langName, out var existing))
                {
                    languageData[langName] = (langColor, existing.Size + size, existing.Count + 1);
                }
                else
                {
                    languageData[langName] = (langColor, size, 1);
                }
            }
        }

        // Apply weights and sort
        var languages = languageData
            .Select(kvp => new
            {
                Name = kvp.Key,
                kvp.Value.Color,
                OriginalSize = kvp.Value.Size,
                Count = kvp.Value.Count,
                WeightedSize = Math.Pow(kvp.Value.Size, sizeWeight) * Math.Pow(kvp.Value.Count, countWeight)
            })
            .OrderByDescending(l => l.WeightedSize)
            .ToList();

        var totalSize = languages.Sum(l => l.OriginalSize);

        var result = languages.Select(l => new LanguageStats
        {
            Name = l.Name,
            Color = l.Color,
            Size = l.OriginalSize,
            RepoCount = l.Count,
            Percentage = totalSize > 0 ? (double)l.OriginalSize / totalSize * 100 : 0
        }).ToList();

        return new TopLanguages
        {
            Languages = result,
            TotalSize = totalSize
        };
    }

    public async Task<Gist> GetGistAsync(
        string gistId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gistId))
        {
            throw new MissingParameterException(["id"]);
        }

        var variables = new Dictionary<string, object?>
        {
            ["gistName"] = gistId
        };

        var response = await ExecuteGraphQLAsync<GistResponse>(
            GraphQLQueries.GistQuery,
            variables,
            cancellationToken);

        var gistData = response.Data?.Viewer?.Gist;
        if (gistData == null)
        {
            throw new GistNotFoundException(gistId);
        }

        // Calculate primary language by file size
        var languageSizes = new Dictionary<string, long>();
        foreach (var file in gistData.Files)
        {
            if (file.Language?.Name != null)
            {
                if (languageSizes.TryGetValue(file.Language.Name, out var size))
                {
                    languageSizes[file.Language.Name] = size + file.Size;
                }
                else
                {
                    languageSizes[file.Language.Name] = file.Size;
                }
            }
        }

        var primaryLanguage = languageSizes
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .FirstOrDefault();

        var firstFileName = gistData.Files.FirstOrDefault()?.Name ?? gistId;

        return new Gist
        {
            Name = firstFileName,
            NameWithOwner = $"{gistData.Owner.Login}/{firstFileName}",
            Description = gistData.Description,
            Language = primaryLanguage,
            StarsCount = gistData.StargazerCount,
            ForksCount = gistData.Forks.TotalCount
        };
    }

    private async Task<int> FetchTotalCommitsAsync(string username, CancellationToken cancellationToken)
    {
        var token = _tokenRotator.GetNextToken();
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_options.RestApiEndpoint}/search/commits?q=author:{username}");
        request.Headers.Add("Authorization", $"token {token}");
        request.Headers.Add("Accept", "application/vnd.github.cloak-preview");
        request.Headers.Add("User-Agent", "GitHubStats");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CommitSearchResponse>(JsonOptions, cancellationToken);
        return result?.TotalCount ?? 0;
    }

    private async Task<GraphQLResponse<T>> ExecuteGraphQLAsync<T>(
        string query,
        Dictionary<string, object?> variables,
        CancellationToken cancellationToken)
    {
        var token = _tokenRotator.GetNextToken();
        var request = new HttpRequestMessage(HttpMethod.Post, _options.GraphQLEndpoint);
        request.Headers.Add("Authorization", $"bearer {token}");
        request.Headers.Add("User-Agent", "GitHubStats");

        var payload = new { query, variables };
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<GraphQLResponse<T>>(content, JsonOptions);

        if (result == null)
        {
            throw new DomainException("Failed to parse GitHub API response", "PARSE_ERROR");
        }

        // Check for GraphQL errors
        if (result.Errors?.Count > 0)
        {
            var error = result.Errors[0];
            // NOT_FOUND errors are handled by the caller based on null data
            // This allows proper distinction between user/repo not found
            if (error.Type == "NOT_FOUND")
            {
                _logger.LogDebug("GraphQL NOT_FOUND: {Message}", error.Message);
                return result; // Let caller handle null data
            }
            if (error.Type == "RATE_LIMITED" || error.Message?.Contains("rate limit", StringComparison.OrdinalIgnoreCase) == true)
            {
                _tokenRotator.MarkRateLimited(token, DateTime.UtcNow.AddMinutes(5));
                throw new RateLimitException();
            }

            _logger.LogWarning("GraphQL error: {Type} - {Message}", error.Type, error.Message);
            throw new DomainException(error.Message ?? "GraphQL query failed", "GRAPHQL_ERROR");
        }

        return result;
    }
}

#region Response DTOs

internal sealed class GraphQLResponse<T>
{
    public T? Data { get; set; }
    public List<GraphQLError>? Errors { get; set; }
}

internal sealed class GraphQLError
{
    public string? Type { get; set; }
    public string? Message { get; set; }
}

internal sealed class UserStatsResponse
{
    public UserData? User { get; set; }
}

internal sealed class UserData
{
    public string? Name { get; set; }
    public required string Login { get; set; }
    public required ContributionsData Commits { get; set; }
    public required ReviewsData Reviews { get; set; }
    public required CountData RepositoriesContributedTo { get; set; }
    public required CountData PullRequests { get; set; }
    public CountData? MergedPullRequests { get; set; }
    public required CountData OpenIssues { get; set; }
    public required CountData ClosedIssues { get; set; }
    public required CountData Followers { get; set; }
    public CountData? RepositoryDiscussions { get; set; }
    public CountData? RepositoryDiscussionComments { get; set; }
    public required RepositoriesData Repositories { get; set; }
}

internal sealed class ContributionsData
{
    public int TotalCommitContributions { get; set; }
}

internal sealed class ReviewsData
{
    public int TotalPullRequestReviewContributions { get; set; }
}

internal sealed class CountData
{
    public int TotalCount { get; set; }
}

internal sealed class RepositoriesData
{
    public int TotalCount { get; set; }
    public required List<RepoNode> Nodes { get; set; }
    public required PageInfo PageInfo { get; set; }
}

internal sealed class RepoNode
{
    public required string Name { get; set; }
    public required StargazersData Stargazers { get; set; }
}

internal sealed class StargazersData
{
    public int TotalCount { get; set; }
}

internal sealed class PageInfo
{
    public bool HasNextPage { get; set; }
    public string? EndCursor { get; set; }
}

internal sealed class RepositoryResponse
{
    public UserRepoData? User { get; set; }
    public OrgRepoData? Organization { get; set; }
}

internal sealed class UserRepoData
{
    public RepoDetail? Repository { get; set; }
}

internal sealed class OrgRepoData
{
    public RepoDetail? Repository { get; set; }
}

internal sealed class RepoDetail
{
    public required string Name { get; set; }
    public required string NameWithOwner { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsArchived { get; set; }
    public bool IsTemplate { get; set; }
    public required StargazersData Stargazers { get; set; }
    public string? Description { get; set; }
    public LanguageData? PrimaryLanguage { get; set; }
    public int ForkCount { get; set; }
}

internal sealed class LanguageData
{
    public required string Name { get; set; }
    public string? Color { get; set; }
}

internal sealed class TopLanguagesResponse
{
    public TopLangsUserData? User { get; set; }
}

internal sealed class TopLangsUserData
{
    public required TopLangsRepositoriesData Repositories { get; set; }
}

internal sealed class TopLangsRepositoriesData
{
    public required List<TopLangsRepoNode> Nodes { get; set; }
}

internal sealed class TopLangsRepoNode
{
    public required string Name { get; set; }
    public required TopLangsLanguagesData Languages { get; set; }
}

internal sealed class TopLangsLanguagesData
{
    public required List<LanguageEdge> Edges { get; set; }
}

internal sealed class LanguageEdge
{
    public long Size { get; set; }
    public required LanguageNode Node { get; set; }
}

internal sealed class LanguageNode
{
    public required string Name { get; set; }
    public string? Color { get; set; }
}

internal sealed class GistResponse
{
    public ViewerData? Viewer { get; set; }
}

internal sealed class ViewerData
{
    public GistData? Gist { get; set; }
}

internal sealed class GistData
{
    public string? Description { get; set; }
    public required OwnerData Owner { get; set; }
    public int StargazerCount { get; set; }
    public required ForksData Forks { get; set; }
    public required List<GistFile> Files { get; set; }
}

internal sealed class OwnerData
{
    public required string Login { get; set; }
}

internal sealed class ForksData
{
    public int TotalCount { get; set; }
}

internal sealed class GistFile
{
    public required string Name { get; set; }
    public GistLanguage? Language { get; set; }
    public long Size { get; set; }
}

internal sealed class GistLanguage
{
    public string? Name { get; set; }
}

internal sealed class CommitSearchResponse
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
}

#endregion
