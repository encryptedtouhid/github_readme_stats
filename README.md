# GitHub Readme Stats

A .NET 9 Web API that generates customizable SVG cards displaying GitHub user and repository statistics for embedding in README files.

## Features

- **User Stats Card** - Stars, commits, PRs, reviews, followers, and rank
- **Top Languages Card** - Most used programming languages
- **Repository Card** - Pinned repository information
- **Gist Card** - Gist statistics
- **WakaTime Card** - Coding activity stats

## Prerequisites

- .NET 9.0 SDK
- Redis (optional, for distributed caching)

## Getting Started

```bash
# Build
cd src
dotnet build GitHubStats.sln

# Run
cd GitHubStats.Api
dotnet run
```

The API will be available at `http://localhost:5042`.

## API Endpoints

| Endpoint | Description |
|----------|-------------|
| `/api/stats?username={user}` | User statistics card |
| `/api/top-langs?username={user}` | Top languages card |
| `/api/repos?username={user}&repo={repo}` | Repository card |
| `/api/gists?username={user}` | Gist card |
| `/api/wakatime?username={user}` | WakaTime stats card |
| `/health` | Health check |

## Configuration

Configure via `appsettings.json` or environment variables:

- **GitHub API** - Token and endpoint settings
- **Redis** - Connection string for distributed caching
- **Cache TTL** - Customizable cache duration per card type
- **Access Control** - Whitelist/blacklist for users and repositories

## License

MIT
