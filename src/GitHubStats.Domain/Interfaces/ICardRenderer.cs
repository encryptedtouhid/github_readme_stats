using GitHubStats.Domain.Entities;

namespace GitHubStats.Domain.Interfaces;

/// <summary>
/// Interface for SVG card rendering operations.
/// </summary>
public interface ICardRenderer
{
    /// <summary>
    /// Renders a stats card SVG.
    /// </summary>
    string RenderStatsCard(UserStats stats, StatsCardOptions options);

    /// <summary>
    /// Renders a repository card SVG.
    /// </summary>
    string RenderRepoCard(Repository repo, RepoCardOptions options);

    /// <summary>
    /// Renders a top languages card SVG.
    /// </summary>
    string RenderTopLanguagesCard(TopLanguages languages, TopLanguagesCardOptions options);

    /// <summary>
    /// Renders a gist card SVG.
    /// </summary>
    string RenderGistCard(Gist gist, GistCardOptions options);

    /// <summary>
    /// Renders a WakaTime card SVG.
    /// </summary>
    string RenderWakaTimeCard(WakaTimeStats stats, WakaTimeCardOptions options);

    /// <summary>
    /// Renders an error card SVG.
    /// </summary>
    string RenderErrorCard(string message, string? secondaryMessage = null, CardColors? colors = null);
}

/// <summary>
/// Base card options with common styling properties.
/// </summary>
public record CardOptions
{
    public string? Theme { get; init; }
    public string? TitleColor { get; init; }
    public string? TextColor { get; init; }
    public string? IconColor { get; init; }
    public string? BgColor { get; init; }
    public string? BorderColor { get; init; }
    public double? BorderRadius { get; init; }
    public bool HideBorder { get; init; }
    public bool HideTitle { get; init; }
    public string? CustomTitle { get; init; }
    public string? Locale { get; init; }
    public bool DisableAnimations { get; init; }
}

/// <summary>
/// Card color configuration.
/// </summary>
public record CardColors
{
    public string TitleColor { get; init; } = "2f80ed";
    public string TextColor { get; init; } = "434d58";
    public string IconColor { get; init; } = "4c71f2";
    public string BgColor { get; init; } = "fffefe";
    public string BorderColor { get; init; } = "e4e2e2";
    public string? RingColor { get; init; }
}

/// <summary>
/// Stats card specific options.
/// </summary>
public record StatsCardOptions : CardOptions
{
    public IReadOnlyList<string>? Hide { get; init; }
    public IReadOnlyList<string>? Show { get; init; }
    public bool ShowIcons { get; init; }
    public bool HideRank { get; init; }
    public bool IncludeAllCommits { get; init; }
    public int? CommitsYear { get; init; }
    public int? LineHeight { get; init; }
    public int? CardWidth { get; init; }
    public string? RingColor { get; init; }
    public bool TextBold { get; init; } = true;
    public string NumberFormat { get; init; } = "short";
    public int? NumberPrecision { get; init; }
    public string RankIcon { get; init; } = "default";
}

/// <summary>
/// Repository card specific options.
/// </summary>
public record RepoCardOptions : CardOptions
{
    public bool ShowOwner { get; init; }
    public int? DescriptionLinesCount { get; init; }
}

/// <summary>
/// Top languages card specific options.
/// </summary>
public record TopLanguagesCardOptions : CardOptions
{
    public IReadOnlyList<string>? Hide { get; init; }
    public string Layout { get; init; } = "normal";
    public int? LangsCount { get; init; }
    public int? CardWidth { get; init; }
    public bool HideProgress { get; init; }
    public string StatsFormat { get; init; } = "percentages";
}

/// <summary>
/// Gist card specific options.
/// </summary>
public record GistCardOptions : CardOptions
{
    public bool ShowOwner { get; init; }
}

/// <summary>
/// WakaTime card specific options.
/// </summary>
public record WakaTimeCardOptions : CardOptions
{
    public IReadOnlyList<string>? Hide { get; init; }
    public string Layout { get; init; } = "default";
    public int? LangsCount { get; init; }
    public int? CardWidth { get; init; }
    public int? LineHeight { get; init; }
    public bool HideProgress { get; init; }
    public string DisplayFormat { get; init; } = "time";
}
