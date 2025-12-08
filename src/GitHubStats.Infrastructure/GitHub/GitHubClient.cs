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

    public async Task<StreakStats> GetStreakStatsAsync(
        string username,
        int? startingYear = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new MissingParameterException(["username"]);
        }

        var currentYear = DateTime.UtcNow.Year;
        var minYear = startingYear ?? 2005; // Git was created in 2005

        // First, fetch the user's creation date to optimize the year range
        var userCreatedYear = await GetUserCreatedYearAsync(username, cancellationToken);
        if (!startingYear.HasValue && userCreatedYear.HasValue)
        {
            minYear = Math.Max(minYear, userCreatedYear.Value);
        }

        // Build list of years to fetch
        var yearsToFetch = Enumerable.Range(minYear, currentYear - minYear + 1).ToList();

        // Use batched GraphQL queries - fetch up to 5 years per request in parallel
        const int batchSize = 5;
        var batches = yearsToFetch
            .Select((year, index) => new { year, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.year).ToList())
            .ToList();

        // Execute batched queries in parallel
        var batchTasks = batches.Select(batch => FetchBatchedContributionsAsync(username, batch, currentYear, cancellationToken)).ToList();
        var batchResults = await Task.WhenAll(batchTasks);

        // Aggregate results - contributions are already sorted per year, merge them
        var allContributions = new List<(DateOnly Date, int Count)>(yearsToFetch.Count * 366);
        var totalContributions = 0;

        foreach (var batchResult in batchResults.Where(r => r != null))
        {
            foreach (var yearResult in batchResult!)
            {
                totalContributions += yearResult.TotalContributions;
                allContributions.AddRange(yearResult.Contributions);
            }
        }

        // Sort once after merging all years (required since batched fetch doesn't guarantee order)
        allContributions.Sort((a, b) => a.Date.CompareTo(b.Date));

        // Calculate streaks
        var (currentStreak, longestStreak, firstContribution) = CalculateStreaks(allContributions);

        return new StreakStats
        {
            Username = username,
            TotalContributions = totalContributions,
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak,
            FirstContribution = firstContribution
        };
    }

    private async Task<int?> GetUserCreatedYearAsync(string username, CancellationToken cancellationToken)
    {
        try
        {
            var variables = new Dictionary<string, object?>
            {
                ["login"] = username
            };

            var response = await ExecuteGraphQLAsync<UserCreatedAtResponse>(
                GraphQLQueries.UserCreatedAtQuery,
                variables,
                cancellationToken);

            if (response.Data?.User?.CreatedAt != null &&
                DateTime.TryParse(response.Data.User.CreatedAt, out var createdDate))
            {
                return createdDate.Year;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch user creation date for {Username}", username);
        }

        return null;
    }

    private async Task<List<(int TotalContributions, List<(DateOnly Date, int Count)> Contributions)>?> FetchBatchedContributionsAsync(
        string username,
        List<int> years,
        int currentYear,
        CancellationToken cancellationToken)
    {
        try
        {
            // Build year ranges for the batched query
            var yearRanges = years.Select(year =>
            {
                var fromDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var toDate = year == currentYear
                    ? DateTime.UtcNow
                    : new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                return (year, fromDate.ToString("yyyy-MM-ddTHH:mm:ssZ"), toDate.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }).ToList();

            var query = GraphQLQueries.GenerateBatchedContributionQuery(yearRanges);
            var variables = new Dictionary<string, object?>
            {
                ["login"] = username
            };

            var response = await ExecuteGraphQLAsync<BatchedContributionResponse>(query, variables, cancellationToken);

            if (response.Data?.User == null)
            {
                throw new UserNotFoundException(username);
            }

            var results = new List<(int TotalContributions, List<(DateOnly Date, int Count)> Contributions)>();

            foreach (var year in years)
            {
                var yearKey = $"y{year}";
                if (response.Data.User.YearlyContributions.TryGetValue(yearKey, out var collection) && collection != null)
                {
                    var calendar = collection.ContributionCalendar;
                    var contributions = new List<(DateOnly Date, int Count)>(calendar.Weeks.Count * 7);

                    foreach (var week in calendar.Weeks)
                    {
                        foreach (var day in week.ContributionDays)
                        {
                            if (DateOnly.TryParse(day.Date, out var date))
                            {
                                contributions.Add((date, day.ContributionCount));
                            }
                        }
                    }

                    results.Add((calendar.TotalContributions, contributions));
                }
            }

            return results;
        }
        catch (UserNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch batched contributions for years {Years}", string.Join(", ", years));
            return null;
        }
    }

    private static (StreakInfo Current, StreakInfo Longest, DateOnly? FirstContribution) CalculateStreaks(
        List<(DateOnly Date, int Count)> contributions)
    {
        if (contributions.Count == 0)
        {
            return (
                new StreakInfo { Length = 0 },
                new StreakInfo { Length = 0 },
                null);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Data is already sorted from the caller, just need to dedupe by taking max count per date
        // Use a dictionary for O(1) lookup instead of GroupBy which is O(n)
        var contributionsByDate = new Dictionary<DateOnly, int>(contributions.Count);
        foreach (var (date, count) in contributions)
        {
            if (contributionsByDate.TryGetValue(date, out var existing))
            {
                if (count > existing)
                    contributionsByDate[date] = count;
            }
            else
            {
                contributionsByDate[date] = count;
            }
        }

        // Find first contribution date and check if any contributions exist
        DateOnly? firstContribution = null;
        foreach (var (date, count) in contributions)
        {
            if (count > 0)
            {
                firstContribution = date;
                break;
            }
        }

        if (firstContribution == null)
        {
            return (
                new StreakInfo { Length = 0 },
                new StreakInfo { Length = 0 },
                null);
        }

        // Matching PHP logic: iterate through all dates in order
        // A streak breaks when count == 0 (unless it's today)
        var longestStreak = new StreakInfo { Length = 0 };
        var currentStreak = new StreakInfo { Length = 0 };

        DateOnly? currentStart = null;
        DateOnly? currentEnd = null;
        var currentLength = 0;

        // Get sorted unique dates for iteration
        var sortedDates = contributionsByDate.Keys.ToList();
        sortedDates.Sort();

        foreach (var date in sortedDates)
        {
            var count = contributionsByDate[date];

            if (count > 0)
            {
                // Has contribution - increment streak
                currentLength++;
                currentEnd = date;

                // Set start on first day of streak
                if (currentLength == 1)
                {
                    currentStart = date;
                }

                // Update longest if current streak is longer
                if (currentLength > longestStreak.Length)
                {
                    longestStreak = new StreakInfo
                    {
                        Length = currentLength,
                        Start = currentStart,
                        End = currentEnd
                    };
                }
            }
            else if (date != today)
            {
                // No contribution and not today - reset streak
                currentLength = 0;
                currentStart = today;
                currentEnd = today;
            }
            // If count == 0 and date == today, don't break the streak
        }

        // Current streak is valid if it ends today or yesterday
        if (currentLength > 0 && currentEnd.HasValue)
        {
            var yesterday = today.AddDays(-1);
            if (currentEnd.Value >= yesterday)
            {
                currentStreak = new StreakInfo
                {
                    Length = currentLength,
                    Start = currentStart,
                    End = currentEnd
                };
            }
        }

        return (
            currentStreak,
            longestStreak,
            firstContribution);
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

internal sealed class ContributionCalendarResponse
{
    public ContributionUserData? User { get; set; }
}

internal sealed class ContributionUserData
{
    public required ContributionsCollectionData ContributionsCollection { get; set; }
}

internal sealed class UserCreatedAtResponse
{
    public UserCreatedAtData? User { get; set; }
}

internal sealed class UserCreatedAtData
{
    public string? CreatedAt { get; set; }
}

internal sealed class BatchedContributionResponse
{
    public BatchedContributionUserData? User { get; set; }
}

internal sealed class BatchedContributionUserData
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    private Dictionary<string, ContributionsCollectionData?>? _yearlyContributions;

    public Dictionary<string, ContributionsCollectionData?> YearlyContributions
    {
        get
        {
            if (_yearlyContributions != null)
                return _yearlyContributions;

            _yearlyContributions = new Dictionary<string, ContributionsCollectionData?>();

            if (ExtensionData != null)
            {
                foreach (var (key, value) in ExtensionData)
                {
                    if (key.StartsWith("y") && char.IsDigit(key[1]))
                    {
                        try
                        {
                            var collection = value.Deserialize<ContributionsCollectionData>(new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                            _yearlyContributions[key] = collection;
                        }
                        catch
                        {
                            _yearlyContributions[key] = null;
                        }
                    }
                }
            }

            return _yearlyContributions;
        }
    }
}

internal sealed class ContributionsCollectionData
{
    public required ContributionCalendarData ContributionCalendar { get; set; }
}

internal sealed class ContributionCalendarData
{
    public int TotalContributions { get; set; }
    public required List<ContributionWeekData> Weeks { get; set; }
}

internal sealed class ContributionWeekData
{
    public required List<ContributionDayData> ContributionDays { get; set; }
}

internal sealed class ContributionDayData
{
    public int ContributionCount { get; set; }
    public required string Date { get; set; }
}

#endregion
