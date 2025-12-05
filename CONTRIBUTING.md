# Contributing to GitHub Readme Stats

Thank you for your interest in contributing! This document provides guidelines for contributing to the project.

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Git
- A GitHub Personal Access Token (for API access)

### Setup

1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR_USERNAME/github_readme_stats.git
   cd github_readme_stats
   ```

3. Build the project:
   ```bash
   cd src
   dotnet build GitHubStats.sln
   ```

4. Configure your GitHub token in `appsettings.Development.json`:
   ```json
   {
     "GitHub": {
       "PersonalAccessTokens": ["your_github_token"]
     }
   }
   ```

5. Run the API:
   ```bash
   cd GitHubStats.Api
   dotnet run
   ```

## Project Structure

```
src/
├── GitHubStats.Api/            # Web API endpoints
├── GitHubStats.Application/    # Business logic and services
├── GitHubStats.Domain/         # Core entities and interfaces
├── GitHubStats.Infrastructure/ # External services (GitHub API, caching)
└── GitHubStats.Rendering/      # SVG card rendering and themes
```

## How to Contribute

### Reporting Bugs

- Check existing issues first
- Include steps to reproduce
- Include expected vs actual behavior
- Include .NET version and OS

### Suggesting Features

- Open an issue with the "feature request" label
- Describe the feature and its use case
- Explain why it would be useful

### Pull Requests

1. Create a feature branch:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. Make your changes following the coding standards

3. Test your changes:
   ```bash
   dotnet build
   dotnet run
   ```

4. Commit with a clear message:
   ```bash
   git commit -m "Add: description of your change"
   ```

5. Push and create a pull request

## Adding a New Theme

Themes are defined in `GitHubStats.Rendering/Themes/ThemeManager.cs`.

1. Add your theme to the `Themes` dictionary:
   ```csharp
   ["your_theme_name"] = new CardColors
   {
       TitleColor = "HEXCOLOR",    // Without #
       TextColor = "HEXCOLOR",
       IconColor = "HEXCOLOR",
       BgColor = "HEXCOLOR",
       BorderColor = "HEXCOLOR",
       RingColor = "HEXCOLOR"      // Optional, for streak cards
   }
   ```

2. Update `README.md` to include your theme in the appropriate category

3. Test your theme:
   ```
   http://localhost:5042/api/streak?username=torvalds&theme=your_theme_name
   ```

## Adding a New Card Type

1. Create the entity in `GitHubStats.Domain/Entities/`
2. Add the interface in `GitHubStats.Domain/Interfaces/`
3. Implement the service in `GitHubStats.Application/Services/`
4. Add the renderer method in `GitHubStats.Rendering/Cards/CardRenderer.cs`
5. Create the endpoint in `GitHubStats.Api/Endpoints/`
6. Register services in the appropriate `ServiceCollectionExtensions.cs`

## Coding Standards

- Use C# 12 features where appropriate
- Follow Microsoft's C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and small

## Code Style

- Indent with 4 spaces
- Use file-scoped namespaces
- Use `var` when the type is obvious
- Prefer expression-bodied members for simple methods
- Use nullable reference types

## Commit Message Format

Use clear, descriptive commit messages:

- `Add:` for new features
- `Fix:` for bug fixes
- `Update:` for enhancements
- `Remove:` for deletions
- `Refactor:` for code restructuring
- `Docs:` for documentation changes

## Testing

Before submitting a PR:

1. Ensure the project builds without errors:
   ```bash
   dotnet build
   ```

2. Test the endpoints manually:
   ```bash
   curl http://localhost:5042/api/streak?username=torvalds
   curl http://localhost:5042/api/stats?username=torvalds
   curl http://localhost:5042/health
   ```

3. Verify your changes work with different themes

## Questions?

Feel free to open an issue for any questions about contributing.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
