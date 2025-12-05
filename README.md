# GitHub Readme Stats

A .NET 9 Web API that generates customizable SVG cards displaying GitHub user and repository statistics for embedding in README files.

## Features

- **Streak Stats Card** - Current streak, longest streak, and total contributions with animated SVG
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
| `/api/streak?username={user}` | Streak statistics card |
| `/api/stats?username={user}` | User statistics card |
| `/api/top-langs?username={user}` | Top languages card |
| `/api/pin?username={user}&repo={repo}` | Repository pin card |
| `/api/gist?id={gist_id}` | Gist card |
| `/api/wakatime?username={user}` | WakaTime stats card |
| `/health` | Health check |

## Usage

Add to your GitHub README:

```markdown
![GitHub Streak](https://your-domain.com/api/streak?username=YOUR_USERNAME)
```

### Streak Card Options

| Parameter | Description | Default |
|-----------|-------------|---------|
| `username` | GitHub username (required) | - |
| `theme` | Card theme | `default` |
| `hide_border` | Hide card border | `false` |
| `border_radius` | Card corner radius | `4.5` |

### Available Themes

- `default` - Dark theme with coral accent
- `aurora` - Purple/teal gradient theme
- `neon` - Cyan/magenta cyberpunk theme

## Configuration

Configure via `appsettings.json` or environment variables:

- **GitHub API** - Token and endpoint settings
- **Redis** - Connection string for distributed caching
- **Cache TTL** - Customizable cache duration per card type
- **Access Control** - Whitelist/blacklist for users and repositories

## License

MIT
