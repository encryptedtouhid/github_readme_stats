namespace GitHubStats.Infrastructure.GitHub;

/// <summary>
/// GraphQL queries for GitHub API.
/// </summary>
public static class GraphQLQueries
{
    public const string UserStatsQuery = """
        query userInfo($login: String!, $after: String, $includeMergedPullRequests: Boolean!, $includeDiscussions: Boolean!, $includeDiscussionsAnswers: Boolean!, $startTime: DateTime = null) {
            user(login: $login) {
                name
                login
                commits: contributionsCollection(from: $startTime) {
                    totalCommitContributions
                }
                reviews: contributionsCollection {
                    totalPullRequestReviewContributions
                }
                repositoriesContributedTo(first: 1, contributionTypes: [COMMIT, ISSUE, PULL_REQUEST, REPOSITORY]) {
                    totalCount
                }
                pullRequests(first: 1) {
                    totalCount
                }
                mergedPullRequests: pullRequests(states: MERGED) @include(if: $includeMergedPullRequests) {
                    totalCount
                }
                openIssues: issues(states: OPEN) {
                    totalCount
                }
                closedIssues: issues(states: CLOSED) {
                    totalCount
                }
                followers {
                    totalCount
                }
                repositoryDiscussions @include(if: $includeDiscussions) {
                    totalCount
                }
                repositoryDiscussionComments(onlyAnswers: true) @include(if: $includeDiscussionsAnswers) {
                    totalCount
                }
                repositories(first: 100, ownerAffiliations: OWNER, orderBy: {direction: DESC, field: STARGAZERS}, after: $after) {
                    totalCount
                    nodes {
                        name
                        stargazers {
                            totalCount
                        }
                    }
                    pageInfo {
                        hasNextPage
                        endCursor
                    }
                }
            }
        }
        """;

    public const string ReposPaginationQuery = """
        query userInfo($login: String!, $after: String) {
            user(login: $login) {
                repositories(first: 100, ownerAffiliations: OWNER, orderBy: {direction: DESC, field: STARGAZERS}, after: $after) {
                    totalCount
                    nodes {
                        name
                        stargazers {
                            totalCount
                        }
                    }
                    pageInfo {
                        hasNextPage
                        endCursor
                    }
                }
            }
        }
        """;

    public const string RepositoryQuery = """
        fragment RepoInfo on Repository {
            name
            nameWithOwner
            isPrivate
            isArchived
            isTemplate
            stargazers {
                totalCount
            }
            description
            primaryLanguage {
                color
                id
                name
            }
            forkCount
        }
        query getRepo($login: String!, $repo: String!) {
            user(login: $login) {
                repository(name: $repo) {
                    ...RepoInfo
                }
            }
            organization(login: $login) {
                repository(name: $repo) {
                    ...RepoInfo
                }
            }
        }
        """;

    public const string TopLanguagesQuery = """
        query userInfo($login: String!) {
            user(login: $login) {
                repositories(ownerAffiliations: OWNER, isFork: false, first: 100, orderBy: {field: UPDATED_AT, direction: DESC}) {
                    nodes {
                        name
                        isArchived
                        languages(first: 10, orderBy: {field: SIZE, direction: DESC}) {
                            edges {
                                size
                                node {
                                    color
                                    name
                                }
                            }
                        }
                    }
                }
            }
        }
        """;

    public const string GistQuery = """
        query gistInfo($gistName: String!) {
            viewer {
                gist(name: $gistName) {
                    description
                    owner {
                        login
                    }
                    stargazerCount
                    forks {
                        totalCount
                    }
                    files {
                        name
                        language {
                            name
                        }
                        size
                    }
                }
            }
        }
        """;

    public const string ContributionCalendarQuery = """
        query contributionCalendar($login: String!, $from: DateTime!, $to: DateTime!) {
            user(login: $login) {
                contributionsCollection(from: $from, to: $to) {
                    contributionCalendar {
                        totalContributions
                        weeks {
                            contributionDays {
                                contributionCount
                                date
                            }
                        }
                    }
                }
            }
        }
        """;

    /// <summary>
    /// Lightweight query to fetch only user creation date for optimizing year range.
    /// </summary>
    public const string UserCreatedAtQuery = """
        query userCreatedAt($login: String!) {
            user(login: $login) {
                createdAt
            }
        }
        """;

    /// <summary>
    /// Generates a batched GraphQL query to fetch multiple years of contribution data in a single request.
    /// Uses GraphQL aliases to fetch up to 5 years per request, dramatically reducing API calls.
    /// </summary>
    public static string GenerateBatchedContributionQuery(IReadOnlyList<(int Year, string From, string To)> yearRanges)
    {
        if (yearRanges.Count == 0)
            return ContributionCalendarQuery;

        var sb = new System.Text.StringBuilder(2048);
        sb.Append("query batchedContributions($login: String!) { user(login: $login) {");

        foreach (var (year, from, to) in yearRanges)
        {
            sb.Append($" y{year}: contributionsCollection(from: \"{from}\", to: \"{to}\") {{ contributionCalendar {{ totalContributions weeks {{ contributionDays {{ contributionCount date }} }} }} }}");
        }

        sb.Append(" } }");
        return sb.ToString();
    }
}
