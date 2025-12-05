using System.Web;
using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Interfaces;
using GitHubStats.Rendering.Common;
using GitHubStats.Rendering.Themes;

namespace GitHubStats.Rendering.Cards;

/// <summary>
/// Main card renderer implementation.
/// </summary>
public sealed class CardRenderer : ICardRenderer
{
    public string RenderStatsCard(UserStats stats, StatsCardOptions options)
    {
        var colors = ThemeManager.GetColors(
            options.Theme,
            options.TitleColor,
            options.TextColor,
            options.IconColor,
            options.BgColor,
            options.BorderColor,
            options.RingColor);

        var card = new Card
        {
            Width = options.CardWidth ?? (options.HideRank ? 287 : 450),
            Height = CalculateStatsCardHeight(options),
            BorderRadius = options.BorderRadius ?? 4.5,
            Colors = colors,
            Title = options.CustomTitle ?? $"{stats.Name}'s GitHub Stats",
            HideBorder = options.HideBorder,
            HideTitle = options.HideTitle,
            DisableAnimations = options.DisableAnimations,
            A11yTitle = $"GitHub Stats for {stats.Name}",
            A11yDesc = $"Total Stars: {stats.TotalStars}, Total Commits: {stats.TotalCommits}"
        };

        card.CustomCss = GetStatsCardCss(colors, options);
        return card.Render(RenderStatsCardBody(stats, options, colors));
    }

    public string RenderRepoCard(Repository repo, RepoCardOptions options)
    {
        var colors = ThemeManager.GetColors(
            options.Theme ?? "default_repocard",
            options.TitleColor,
            options.TextColor,
            options.IconColor,
            options.BgColor,
            options.BorderColor);

        var header = options.ShowOwner ? repo.NameWithOwner : repo.Name;
        if (header.Length > 35)
            header = header[..35] + "...";

        var description = repo.Description ?? "No description provided";
        var wrappedDesc = WrapText(description, 59, options.DescriptionLinesCount ?? 3);
        var height = 110 + wrappedDesc.Count * 12;

        var card = new Card
        {
            Width = 400,
            Height = height,
            BorderRadius = options.BorderRadius ?? 4.5,
            Colors = colors,
            Title = header,
            TitlePrefixIcon = Icons.Contribs,
            HideBorder = options.HideBorder,
            HideTitle = false,
            DisableAnimations = true,
            A11yTitle = $"Repository: {repo.NameWithOwner}",
            A11yDesc = description
        };

        card.CustomCss = GetRepoCardCss(colors);
        return card.Render(RenderRepoCardBody(repo, wrappedDesc, height, colors, options));
    }

    public string RenderTopLanguagesCard(TopLanguages languages, TopLanguagesCardOptions options)
    {
        var colors = ThemeManager.GetColors(
            options.Theme,
            options.TitleColor,
            options.TextColor,
            options.IconColor,
            options.BgColor,
            options.BorderColor);

        var langsCount = Math.Min(options.LangsCount ?? 5, 20);
        var filteredLangs = FilterLanguages(languages.Languages, options.Hide, langsCount);

        var (width, height) = CalculateTopLangsCardDimensions(filteredLangs.Count, options);

        var card = new Card
        {
            Width = options.CardWidth ?? width,
            Height = height,
            BorderRadius = options.BorderRadius ?? 4.5,
            Colors = colors,
            Title = options.CustomTitle ?? "Most Used Languages",
            HideBorder = options.HideBorder,
            HideTitle = options.HideTitle,
            DisableAnimations = options.DisableAnimations,
            A11yTitle = "Top Languages",
            A11yDesc = string.Join(", ", filteredLangs.Select(l => l.Name))
        };

        card.CustomCss = GetTopLangsCardCss(colors);
        return card.Render(RenderTopLangsBody(filteredLangs, options, card.Width));
    }

    public string RenderGistCard(Gist gist, GistCardOptions options)
    {
        var colors = ThemeManager.GetColors(
            options.Theme ?? "default_repocard",
            options.TitleColor,
            options.TextColor,
            options.IconColor,
            options.BgColor,
            options.BorderColor);

        var header = options.ShowOwner ? gist.NameWithOwner : gist.Name;
        if (header.Length > 35)
            header = header[..35] + "...";

        var description = gist.Description ?? "No description provided";
        var wrappedDesc = WrapText(description, 59, 3);
        var height = 110 + wrappedDesc.Count * 12;

        var card = new Card
        {
            Width = 400,
            Height = height,
            BorderRadius = options.BorderRadius ?? 4.5,
            Colors = colors,
            Title = header,
            TitlePrefixIcon = Icons.Gist,
            HideBorder = options.HideBorder,
            HideTitle = options.HideTitle,
            DisableAnimations = true,
            A11yTitle = $"Gist: {gist.Name}",
            A11yDesc = description
        };

        card.CustomCss = GetGistCardCss(colors);
        return card.Render(RenderGistCardBody(gist, wrappedDesc, height, colors));
    }

    public string RenderWakaTimeCard(WakaTimeStats stats, WakaTimeCardOptions options)
    {
        var colors = ThemeManager.GetColors(
            options.Theme,
            options.TitleColor,
            options.TextColor,
            options.IconColor,
            options.BgColor,
            options.BorderColor);

        var langsCount = Math.Min(options.LangsCount ?? stats.Languages.Count, 20);
        var filteredLangs = FilterWakaTimeLanguages(stats.Languages, options.Hide, langsCount);

        var lineHeight = options.LineHeight ?? 25;
        var height = Math.Max(45 + (filteredLangs.Count + 1) * lineHeight, 150);

        if (options.Layout == "compact")
        {
            height = 90 + (int)Math.Ceiling(filteredLangs.Count / 2.0) * 25;
        }

        var title = "WakaTime Stats";
        if (stats.Range == "last_7_days")
            title += " (Last 7 Days)";
        else if (stats.Range == "last_year")
            title += " (Last Year)";

        var card = new Card
        {
            Width = options.CardWidth ?? 495,
            Height = height,
            BorderRadius = options.BorderRadius ?? 4.5,
            Colors = colors,
            Title = options.CustomTitle ?? title,
            HideBorder = options.HideBorder,
            HideTitle = options.HideTitle,
            DisableAnimations = options.DisableAnimations,
            A11yTitle = "WakaTime Stats",
            A11yDesc = string.Join(", ", filteredLangs.Select(l => $"{l.Name}: {l.Text}"))
        };

        card.CustomCss = GetWakaTimeCardCss(colors);
        return card.Render(RenderWakaTimeBody(filteredLangs, stats, options, colors, card.Width));
    }

    public string RenderStreakCard(StreakStats stats, StreakCardOptions options)
    {
        var colors = ThemeManager.GetColors(
            options.Theme ?? "default",
            options.TitleColor,
            options.TextColor,
            options.IconColor,
            options.BgColor,
            options.BorderColor,
            options.RingColor);

        var width = options.CardWidth ?? 495;
        // Desired visual height (what user sees)
        var visualHeight = options.CardHeight ?? 195;
        // Card.cs subtracts 30 when HideTitle=true, so we add 30 to compensate
        var cardHeight = visualHeight + 30;

        // Determine visible sections
        var visibleSections = 3;
        if (options.HideTotalContributions) visibleSections--;
        if (options.HideCurrentStreak) visibleSections--;
        if (options.HideLongestStreak) visibleSections--;

        var card = new Card
        {
            Width = width,
            Height = cardHeight, // Use compensated height
            BorderRadius = options.BorderRadius ?? 4.5,
            Colors = colors,
            Title = string.Empty, // Streak card doesn't have a header title
            HideBorder = options.HideBorder,
            HideTitle = true, // Always hide title for streak card
            DisableAnimations = options.DisableAnimations,
            A11yTitle = $"GitHub Streak Stats for {stats.Username}",
            A11yDesc = $"Total Contributions: {stats.TotalContributions}, Current Streak: {stats.CurrentStreak.Length} days, Longest Streak: {stats.LongestStreak.Length} days"
        };

        card.CustomCss = GetStreakCardCss(colors, options);
        // Pass the visual height for internal layout calculations
        return card.Render(RenderStreakCardBody(stats, options, colors, width, visualHeight, visibleSections));
    }

    public string RenderErrorCard(string message, string? secondaryMessage = null, CardColors? colors = null)
    {
        colors ??= ThemeManager.GetColors();

        var card = new Card
        {
            Width = 495,
            Height = 120,
            Colors = colors,
            Title = "Error",
            HideBorder = false,
            HideTitle = false,
            A11yTitle = "Error",
            A11yDesc = message
        };

        card.CustomCss = $".error {{ font: 400 14px 'Segoe UI', Ubuntu, Sans-Serif; fill: #{colors.TextColor}; }}";

        using var body = new SvgBuilder(512);
        body.StartGroup(transform: "translate(25, 20)");
        body.Text(message, 0, 0, "error");
        if (!string.IsNullOrEmpty(secondaryMessage))
        {
            body.Text(secondaryMessage, 0, 25, "error");
        }
        body.EndGroup();

        return card.Render(body.ToString());
    }

    #region Private Methods - Stats Card

    private static int CalculateStatsCardHeight(StatsCardOptions options)
    {
        var lineHeight = options.LineHeight ?? 25;
        var visibleStats = 6; // Base stats count

        if (options.Show?.Contains("reviews") == true) visibleStats++;
        if (options.Show?.Contains("discussions_started") == true) visibleStats++;
        if (options.Show?.Contains("discussions_answered") == true) visibleStats++;
        if (options.Show?.Contains("prs_merged") == true) visibleStats++;

        if (options.Hide != null)
            visibleStats -= options.Hide.Count(h => new[] { "stars", "commits", "prs", "issues", "contribs" }.Contains(h));

        return Math.Max(150, 45 + visibleStats * lineHeight);
    }

    private static string GetStatsCardCss(CardColors colors, StatsCardOptions options)
    {
        var fontWeight = options.TextBold ? "700" : "400";
        return $@"
.stat {{
    font: 600 14px 'Segoe UI', Ubuntu, Sans-Serif;
    fill: #{colors.TextColor};
}}
.stat.bold {{ font-weight: 700; }}
.stagger {{
    opacity: 0;
    animation: fadeInAnimation 0.3s ease-in-out forwards;
}}
.icon {{ fill: #{colors.IconColor}; }}
.rank-text {{
    font: 800 24px 'Segoe UI', Ubuntu, Sans-Serif;
    fill: #{colors.TextColor};
}}
.rank-percentile-text {{
    font: 400 12px 'Segoe UI', Ubuntu, Sans-Serif;
    fill: #{colors.TextColor};
}}
";
    }

    private static string RenderStatsCardBody(UserStats stats, StatsCardOptions options, CardColors colors)
    {
        using var body = new SvgBuilder(4096);

        var showIcons = options.ShowIcons;
        var lineHeight = options.LineHeight ?? 25;
        var y = 0;
        var staggerDelay = 0;

        var statItems = new List<(string Label, string Value, string Icon, string TestId)>
        {
            ("Total Stars Earned", FormatNumber(stats.TotalStars, options.NumberFormat), Icons.Star, "stars"),
            ("Total Commits", FormatNumber(stats.TotalCommits, options.NumberFormat), Icons.Commits, "commits"),
            ("Total PRs", FormatNumber(stats.TotalPRs, options.NumberFormat), Icons.PullRequest, "prs"),
            ("Total Issues", FormatNumber(stats.TotalIssues, options.NumberFormat), Icons.Issues, "issues"),
            ("Contributed to", FormatNumber(stats.ContributedTo, options.NumberFormat), Icons.Contribs, "contribs")
        };

        if (options.Show?.Contains("reviews") == true)
            statItems.Add(("Total Reviews", FormatNumber(stats.TotalReviews, options.NumberFormat), Icons.Reviews, "reviews"));

        // Filter out hidden stats
        if (options.Hide != null)
            statItems = statItems.Where(s => !options.Hide.Contains(s.TestId)).ToList();

        body.StartGroup(transform: "translate(25, 0)");

        foreach (var (label, value, icon, testId) in statItems)
        {
            body.Append($@"<g class=""stagger"" style=""animation-delay: {staggerDelay}ms"" transform=""translate(0, {y})"">");

            if (showIcons)
            {
                body.Append($@"<svg class=""icon"" viewBox=""0 0 16 16"" version=""1.1"" width=""16"" height=""16"" x=""0"" y=""0"">{icon}</svg>");
                body.Append($@"<text class=""stat bold"" x=""25"" y=""12.5"">{HttpUtility.HtmlEncode(label)}:</text>");
            }
            else
            {
                body.Append($@"<text class=""stat bold"" x=""0"" y=""12.5"">{HttpUtility.HtmlEncode(label)}:</text>");
            }

            body.Append($@"<text class=""stat"" x=""{(showIcons ? 190 : 165)}"" y=""12.5"" data-testid=""{testId}"">{value}</text>");
            body.Append("</g>");

            y += lineHeight;
            staggerDelay += 150;
        }

        body.EndGroup();

        // Render rank circle if not hidden
        if (!options.HideRank)
        {
            body.Append(RenderRankCircle(stats.Rank, colors, options));
        }

        return body.ToString();
    }

    private static string RenderRankCircle(UserRank rank, CardColors colors, StatsCardOptions options)
    {
        using var svg = new SvgBuilder(512);
        var cx = options.CardWidth.HasValue ? options.CardWidth.Value - 75 : 375;

        svg.StartGroup(transform: $"translate({cx}, 50)");

        // Background circle
        svg.Circle(0, 0, 40, stroke: $"#{colors.TextColor}", strokeWidth: 1);

        // Progress circle
        var progress = 100 - rank.Percentile;
        var circumference = 2 * Math.PI * 40;
        var dashOffset = circumference * (1 - progress / 100);

        var ringColor = colors.RingColor ?? colors.TitleColor;
        svg.Append($@"<circle cx=""0"" cy=""0"" r=""40"" fill=""none"" stroke=""#{ringColor}"" stroke-width=""5"" stroke-dasharray=""{circumference:F2}"" stroke-dashoffset=""{dashOffset:F2}"" transform=""rotate(-90)"" stroke-linecap=""round""/>");

        // Rank text
        svg.Append($@"<text class=""rank-text"" x=""0"" y=""0"" text-anchor=""middle"" dominant-baseline=""central"">{rank.Level}</text>");

        svg.EndGroup();

        return svg.ToString();
    }

    #endregion

    #region Private Methods - Repo Card

    private static string GetRepoCardCss(CardColors colors)
    {
        return $@"
.description {{ font: 400 13px 'Segoe UI', Ubuntu, Sans-Serif; fill: #{colors.TextColor}; }}
.gray {{ font: 400 12px 'Segoe UI', Ubuntu, Sans-Serif; fill: #{colors.TextColor}; }}
.icon {{ fill: #{colors.IconColor}; }}
.badge {{ font: 600 11px 'Segoe UI', Ubuntu, Sans-Serif; }}
.badge rect {{ opacity: 0.2; }}
";
    }

    private static string RenderRepoCardBody(Repository repo, List<string> wrappedDesc, int height, CardColors colors, RepoCardOptions options)
    {
        using var body = new SvgBuilder(2048);

        // Badge for archived/template
        if (repo.IsArchived || repo.IsTemplate)
        {
            var label = repo.IsTemplate ? "Template" : "Archived";
            body.Append($@"<g class=""badge"" transform=""translate(320, -18)"">
                <rect stroke=""#{colors.TextColor}"" stroke-width=""1"" width=""70"" height=""20"" x=""-12"" y=""-14"" ry=""10"" rx=""10""/>
                <text x=""23"" y=""-5"" text-anchor=""middle"" fill=""#{colors.TextColor}"">{label}</text>
            </g>");
        }

        // Description
        body.Append(@"<text class=""description"" x=""25"" y=""-5"">");
        foreach (var line in wrappedDesc)
        {
            body.Append($@"<tspan dy=""1.2em"" x=""25"">{HttpUtility.HtmlEncode(line)}</tspan>");
        }
        body.Append("</text>");

        // Footer with language, stars, forks
        body.StartGroup(transform: $"translate(30, {height - 75})");

        var x = 0;

        // Language
        if (repo.PrimaryLanguage != null)
        {
            var langColor = repo.PrimaryLanguage.Color ?? "#858585";
            body.Circle(5, 6, 5, fill: langColor);
            body.Append($@"<text class=""gray"" x=""15"" y=""10"">{HttpUtility.HtmlEncode(repo.PrimaryLanguage.Name)}</text>");
            x += (repo.PrimaryLanguage.Name.Length * 8) + 40;
        }

        // Stars
        body.Append($@"<g transform=""translate({x}, 0)"">");
        body.Append($@"<svg class=""icon"" viewBox=""0 0 16 16"" width=""16"" height=""16"">{Icons.Star}</svg>");
        body.Append($@"<text class=""gray"" x=""20"" y=""10"">{FormatNumber(repo.StarCount, "short")}</text>");
        body.Append("</g>");
        x += 60;

        // Forks
        body.Append($@"<g transform=""translate({x}, 0)"">");
        body.Append($@"<svg class=""icon"" viewBox=""0 0 16 16"" width=""16"" height=""16"">{Icons.Fork}</svg>");
        body.Append($@"<text class=""gray"" x=""20"" y=""10"">{FormatNumber(repo.ForkCount, "short")}</text>");
        body.Append("</g>");

        body.EndGroup();

        return body.ToString();
    }

    #endregion

    #region Private Methods - Top Languages

    private static string GetTopLangsCardCss(CardColors colors)
    {
        return $@"
.lang-name {{ font: 400 11px 'Segoe UI', Ubuntu, Sans-Serif; fill: #{colors.TextColor}; }}
.stagger {{ opacity: 0; animation: fadeInAnimation 0.3s ease-in-out forwards; }}
";
    }

    private static List<LanguageStats> FilterLanguages(IReadOnlyList<LanguageStats> langs, IReadOnlyList<string>? hide, int count)
    {
        var filtered = langs.AsEnumerable();
        if (hide?.Count > 0)
        {
            var hideSet = new HashSet<string>(hide, StringComparer.OrdinalIgnoreCase);
            filtered = filtered.Where(l => !hideSet.Contains(l.Name));
        }
        return filtered.Take(count).ToList();
    }

    private static (int width, int height) CalculateTopLangsCardDimensions(int langCount, TopLanguagesCardOptions options)
    {
        var width = options.CardWidth ?? 300;
        int height;

        switch (options.Layout)
        {
            case "compact":
                height = 90 + (int)Math.Ceiling(langCount / 2.0) * 25;
                break;
            case "donut":
                height = 215 + Math.Max(langCount - 5, 0) * 32;
                width = Math.Max(width, 350);
                break;
            case "donut-vertical":
            case "pie":
                height = 300 + (int)Math.Ceiling(langCount / 2.0) * 25;
                break;
            default:
                height = 45 + (langCount + 1) * 40;
                break;
        }

        return (width, height);
    }

    private static string RenderTopLangsBody(List<LanguageStats> langs, TopLanguagesCardOptions options, int width)
    {
        if (langs.Count == 0)
        {
            return @"<text x=""0"" y=""11"" class=""stat bold"">No languages data available</text>";
        }

        var totalSize = langs.Sum(l => l.Size);

        return options.Layout switch
        {
            "compact" => RenderCompactLayout(langs, totalSize, width, options),
            _ => RenderNormalLayout(langs, totalSize, width, options)
        };
    }

    private static string RenderNormalLayout(List<LanguageStats> langs, long totalSize, int width, TopLanguagesCardOptions options)
    {
        using var body = new SvgBuilder(2048);
        body.StartGroup(transform: "translate(25, 0)");

        var y = 0;
        var staggerDelay = 450;

        foreach (var lang in langs)
        {
            var percent = totalSize > 0 ? (double)lang.Size / totalSize * 100 : 0;
            var displayValue = options.StatsFormat == "bytes"
                ? FormatBytes(lang.Size)
                : $"{percent:F2}%";

            body.Append($@"<g class=""stagger"" style=""animation-delay: {staggerDelay}ms"" transform=""translate(0, {y})"">");
            body.Append($@"<text class=""lang-name"" x=""2"" y=""15"">{HttpUtility.HtmlEncode(lang.Name)}</text>");
            body.Append($@"<text class=""lang-name"" x=""{width - 120}"" y=""34"">{displayValue}</text>");

            // Progress bar
            var progressWidth = width - 120;
            var progress = percent * progressWidth / 100;
            body.Rect(0, 25, progressWidth, 8, fill: "#ddd", rx: 5);
            body.Rect(0, 25, progress, 8, fill: lang.Color, rx: 5);

            body.Append("</g>");

            y += 40;
            staggerDelay += 150;
        }

        body.EndGroup();
        return body.ToString();
    }

    private static string RenderCompactLayout(List<LanguageStats> langs, long totalSize, int width, TopLanguagesCardOptions options)
    {
        using var body = new SvgBuilder(2048);

        // Progress bar
        var barWidth = width - 75;
        body.Append($@"<mask id=""rect-mask""><rect x=""0"" y=""0"" width=""{barWidth}"" height=""8"" fill=""white"" rx=""5""/></mask>");

        var progressX = 0.0;
        foreach (var lang in langs)
        {
            var percent = totalSize > 0 ? (double)lang.Size / totalSize : 0;
            var segmentWidth = barWidth * percent;
            if (segmentWidth < 10) segmentWidth = 10;

            body.Rect(progressX, 0, segmentWidth, 8, fill: lang.Color);
            progressX += segmentWidth;
        }

        // Language labels in two columns
        body.StartGroup(transform: "translate(0, 25)");

        var col1Y = 0;
        var col2Y = 0;
        var col2X = width / 2;

        for (var i = 0; i < langs.Count; i++)
        {
            var lang = langs[i];
            var percent = totalSize > 0 ? (double)lang.Size / totalSize * 100 : 0;
            var displayValue = options.StatsFormat == "bytes"
                ? FormatBytes(lang.Size)
                : $"{percent:F2}%";

            var isLeftCol = i % 2 == 0;
            var x = isLeftCol ? 0 : col2X;
            var y = isLeftCol ? col1Y : col2Y;

            body.Circle(x + 5, y + 6, 5, fill: lang.Color);
            body.Append($@"<text class=""lang-name"" x=""{x + 15}"" y=""{y + 10}"">{HttpUtility.HtmlEncode(lang.Name)} {(options.HideProgress ? "" : displayValue)}</text>");

            if (isLeftCol)
                col1Y += 25;
            else
                col2Y += 25;
        }

        body.EndGroup();

        return body.ToString();
    }

    #endregion

    #region Private Methods - Gist Card

    private static string GetGistCardCss(CardColors colors)
    {
        return $@"
.description {{ font: 400 13px 'Segoe UI', Ubuntu, Sans-Serif; fill: #{colors.TextColor}; }}
.gray {{ font: 400 12px 'Segoe UI', Ubuntu, Sans-Serif; fill: #{colors.TextColor}; }}
.icon {{ fill: #{colors.IconColor}; }}
";
    }

    private static string RenderGistCardBody(Gist gist, List<string> wrappedDesc, int height, CardColors colors)
    {
        using var body = new SvgBuilder(1024);

        // Description
        body.Append(@"<text class=""description"" x=""25"" y=""-5"">");
        foreach (var line in wrappedDesc)
        {
            body.Append($@"<tspan dy=""1.2em"" x=""25"">{HttpUtility.HtmlEncode(line)}</tspan>");
        }
        body.Append("</text>");

        // Footer
        body.StartGroup(transform: $"translate(30, {height - 75})");

        var x = 0;

        // Language
        if (!string.IsNullOrEmpty(gist.Language))
        {
            body.Circle(5, 6, 5, fill: "#858585");
            body.Append($@"<text class=""gray"" x=""15"" y=""10"">{HttpUtility.HtmlEncode(gist.Language)}</text>");
            x += (gist.Language.Length * 8) + 40;
        }

        // Stars
        body.Append($@"<g transform=""translate({x}, 0)"">");
        body.Append($@"<svg class=""icon"" viewBox=""0 0 16 16"" width=""16"" height=""16"">{Icons.Star}</svg>");
        body.Append($@"<text class=""gray"" x=""20"" y=""10"">{FormatNumber(gist.StarsCount, "short")}</text>");
        body.Append("</g>");
        x += 60;

        // Forks
        body.Append($@"<g transform=""translate({x}, 0)"">");
        body.Append($@"<svg class=""icon"" viewBox=""0 0 16 16"" width=""16"" height=""16"">{Icons.Fork}</svg>");
        body.Append($@"<text class=""gray"" x=""20"" y=""10"">{FormatNumber(gist.ForksCount, "short")}</text>");
        body.Append("</g>");

        body.EndGroup();

        return body.ToString();
    }

    #endregion

    #region Private Methods - WakaTime Card

    private static string GetWakaTimeCardCss(CardColors colors)
    {
        return $@"
.stat {{ font: 600 14px 'Segoe UI', Ubuntu, Sans-Serif; fill: #{colors.TextColor}; }}
.stat.bold {{ font-weight: 700; }}
.stagger {{ opacity: 0; animation: fadeInAnimation 0.3s ease-in-out forwards; }}
.lang-name {{ font: 400 11px 'Segoe UI', Ubuntu, Sans-Serif; fill: #{colors.TextColor}; }}
";
    }

    private static List<WakaTimeLanguage> FilterWakaTimeLanguages(IReadOnlyList<WakaTimeLanguage> langs, IReadOnlyList<string>? hide, int count)
    {
        var filtered = langs.Where(l => l.Hours > 0 || l.Minutes > 0);
        if (hide?.Count > 0)
        {
            var hideSet = new HashSet<string>(hide, StringComparer.OrdinalIgnoreCase);
            filtered = filtered.Where(l => !hideSet.Contains(l.Name));
        }
        return filtered.Take(count).ToList();
    }

    private static string RenderWakaTimeBody(List<WakaTimeLanguage> langs, WakaTimeStats stats, WakaTimeCardOptions options, CardColors colors, int width)
    {
        if (langs.Count == 0)
        {
            var message = !stats.IsCodingActivityVisible
                ? "Coding Activity not publicly visible"
                : !stats.IsOtherUsageVisible
                    ? "Code details not available"
                    : "No coding activity this week";

            return $@"<text x=""25"" y=""11"" class=""stat bold"" fill=""#{colors.TextColor}"">{message}</text>";
        }

        using var body = new SvgBuilder(2048);
        body.Append(@"<svg x=""0"" y=""0"" width=""100%"">");

        var y = 0;
        var staggerDelay = 450;
        var lineHeight = options.LineHeight ?? 25;

        foreach (var lang in langs)
        {
            var value = options.DisplayFormat == "percent"
                ? $"{lang.Percent:F2} %"
                : lang.Text ?? $"{lang.Hours}h {lang.Minutes}m";

            body.Append($@"<g class=""stagger"" style=""animation-delay: {staggerDelay}ms"" transform=""translate(25, {y})"">");
            body.Append($@"<text class=""stat bold"" y=""12.5"">{HttpUtility.HtmlEncode(lang.Name)}:</text>");

            if (!options.HideProgress)
            {
                var progressWidth = width - 275;
                var progress = lang.Percent * progressWidth / 100;
                body.Rect(110, 4, progressWidth, 8, fill: $"#{colors.TextColor}10", rx: 5);
                body.Rect(110, 4, progress, 8, fill: $"#{colors.TitleColor}", rx: 5);
                body.Append($@"<text class=""stat"" x=""{130 + progressWidth}"" y=""12.5"">{value}</text>");
            }
            else
            {
                body.Append($@"<text class=""stat"" x=""170"" y=""12.5"">{value}</text>");
            }

            body.Append("</g>");

            y += lineHeight;
            staggerDelay += 150;
        }

        body.Append("</svg>");
        return body.ToString();
    }

    #endregion

    #region Utility Methods

    private static List<string> WrapText(string text, int lineWidth, int maxLines)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var currentLine = "";

        foreach (var word in words)
        {
            if (lines.Count >= maxLines)
                break;

            if (currentLine.Length + word.Length + 1 <= lineWidth)
            {
                currentLine += (currentLine.Length > 0 ? " " : "") + word;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                }
                currentLine = word;
            }
        }

        if (!string.IsNullOrEmpty(currentLine) && lines.Count < maxLines)
        {
            lines.Add(currentLine);
        }

        if (lines.Count == maxLines && words.Length > 0)
        {
            var lastLine = lines[^1];
            if (lastLine.Length < lineWidth - 3)
                lines[^1] = lastLine + "...";
        }

        return lines;
    }

    private static string FormatNumber(int value, string format)
    {
        if (format == "long")
            return value.ToString("N0");

        return value switch
        {
            >= 1_000_000 => $"{value / 1_000_000.0:F1}M",
            >= 1_000 => $"{value / 1_000.0:F1}k",
            _ => value.ToString()
        };
    }

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
            >= 1_048_576 => $"{bytes / 1_048_576.0:F2} MB",
            >= 1_024 => $"{bytes / 1_024.0:F2} KB",
            _ => $"{bytes} B"
        };
    }

    #endregion

    #region Private Methods - Streak Card

    private static string GetStreakCardCss(CardColors colors, StreakCardOptions options)
    {
        var sideNumsColor = options.SideNumsColor ?? colors.TextColor;
        var currStreakNumColor = options.CurrStreakNumColor ?? colors.TitleColor;
        var sideLabelsColor = options.SideLabelsColor ?? colors.TextColor;
        var currStreakLabelColor = options.CurrStreakLabelColor ?? colors.TextColor;
        var datesColor = options.DatesColor ?? colors.TextColor;
        var fireColor = options.FireColor ?? "fb8c00";
        var ringColor = options.RingColor ?? colors.RingColor ?? colors.TitleColor;

        return $@"
.stat-value {{ font: 700 28px 'Segoe UI', Ubuntu, Sans-Serif; }}
.stat-value.side {{ fill: #{sideNumsColor}; }}
.stat-value.current {{ fill: #{currStreakNumColor}; }}
.stat-label {{ font: 400 14px 'Segoe UI', Ubuntu, Sans-Serif; }}
.stat-label.side {{ fill: #{sideLabelsColor}; }}
.stat-label.current {{ fill: #{currStreakLabelColor}; }}
.stat-date {{ font: 400 12px 'Segoe UI', Ubuntu, Sans-Serif; fill: #{datesColor}; }}
.fire {{ fill: #{fireColor}; }}
.ring {{ stroke: #{ringColor}; }}
.streak-section {{
    opacity: 0;
    animation: fadeInAnimation 0.4s ease-in-out forwards;
}}
";
    }

    private static string RenderStreakCardBody(StreakStats stats, StreakCardOptions options, CardColors colors, int width, int height, int visibleSections)
    {
        using var body = new SvgBuilder(4096);

        if (visibleSections == 0) return "";

        var sectionWidth = width / visibleSections;
        var currentSectionIndex = 0;

        // Body wrapper adds 25px offset, so adjust Y positions accordingly
        // PHP uses transforms with text y=32, so actual position = transformY + 32
        const int bodyOffset = 25;

        // Height offset for centering when card height differs from default 195
        var heightOffset = (height - 195) / 2.0;

        // Y positions for side sections (PHP: transform y=48/84/114 + text y=32 = 80/116/146)
        var sideNumberY = 48 + heightOffset - bodyOffset + 32;  // 55 → rendered at 80
        var sideLabelY = 84 + heightOffset - bodyOffset + 32;   // 91 → rendered at 116
        var sideDateY = 114 + heightOffset - bodyOffset + 32;   // 121 → rendered at 146

        // Y positions for current streak section
        // Fire and ring don't have the +32 offset (they're SVG elements, not text)
        var currentFireY = 19.5 + heightOffset - bodyOffset;    // -5.5 → rendered at 19.5
        var currentRingY = 71 + heightOffset - bodyOffset;      // 46 → rendered at 71
        // Current streak number is inside ring (transform y=48 + text y=32 = 80)
        var currentNumberY = 48 + heightOffset - bodyOffset + 32; // 55 → rendered at 80
        // Current streak label (transform y=108 + text y=32 = 140)
        var currentLabelY = 108 + heightOffset - bodyOffset + 32; // 115 → rendered at 140
        // Current streak date (transform y=145 + text y=21 = 166)
        var currentDateY = 145 + heightOffset - bodyOffset + 21;  // 141 → rendered at 166

        // Divider Y positions (spanning content area)
        var dividerY1 = 28 + heightOffset - bodyOffset;         // 3 → rendered at 28
        var dividerY2 = 170 + heightOffset - bodyOffset;        // 145 → rendered at 170

        // Stroke color for dividers
        var strokeColor = options.StrokeColor ?? colors.TextColor;

        // Total Contributions (left section)
        if (!options.HideTotalContributions)
        {
            var x = sectionWidth * currentSectionIndex + sectionWidth / 2;
            body.Append(RenderStreakSectionFixed(
                x,
                sideNumberY, sideLabelY, sideDateY,
                stats.TotalContributions.ToString("N0"),
                "Total Contributions",
                FormatDateRange(stats.FirstContribution, DateOnly.FromDateTime(DateTime.UtcNow)),
                "side",
                currentSectionIndex * 200));
            currentSectionIndex++;

            // Add divider if not the last section
            if (currentSectionIndex < visibleSections)
            {
                var dividerX = sectionWidth * currentSectionIndex;
                body.Append($@"<line x1=""{dividerX}"" y1=""{dividerY1:F1}"" x2=""{dividerX}"" y2=""{dividerY2:F1}"" stroke=""#{strokeColor}"" stroke-width=""1"" stroke-opacity=""0.5""/>");
            }
        }

        // Current Streak (center section with fire icon and ring)
        if (!options.HideCurrentStreak)
        {
            var x = sectionWidth * currentSectionIndex + sectionWidth / 2;
            var fireColor = options.FireColor ?? "fb8c00";
            var ringColor = options.RingColor ?? colors.RingColor ?? colors.TitleColor;

            body.Append(RenderCurrentStreakSectionFixed(
                x,
                currentFireY, currentRingY, currentNumberY, currentLabelY, currentDateY,
                stats.CurrentStreak.Length.ToString(),
                "Current Streak",
                FormatDateRange(stats.CurrentStreak.Start, stats.CurrentStreak.End),
                fireColor,
                ringColor,
                currentSectionIndex * 200));
            currentSectionIndex++;

            // Add divider if not the last section
            if (currentSectionIndex < visibleSections)
            {
                var dividerX = sectionWidth * currentSectionIndex;
                body.Append($@"<line x1=""{dividerX}"" y1=""{dividerY1:F1}"" x2=""{dividerX}"" y2=""{dividerY2:F1}"" stroke=""#{strokeColor}"" stroke-width=""1"" stroke-opacity=""0.5""/>");
            }
        }

        // Longest Streak (right section)
        if (!options.HideLongestStreak)
        {
            var x = sectionWidth * currentSectionIndex + sectionWidth / 2;
            body.Append(RenderStreakSectionFixed(
                x,
                sideNumberY, sideLabelY, sideDateY,
                stats.LongestStreak.Length.ToString(),
                "Longest Streak",
                FormatDateRange(stats.LongestStreak.Start, stats.LongestStreak.End),
                "side",
                currentSectionIndex * 200));
        }

        return body.ToString();
    }

    private static string RenderStreakSectionFixed(int x, double numberY, double labelY, double dateY, string value, string label, string dateRange, string cssClass, int animationDelay)
    {
        return $@"
<g class=""streak-section"" style=""animation-delay: {animationDelay}ms"">
    <text class=""stat-value {cssClass}"" x=""{x}"" y=""{numberY:F1}"" text-anchor=""middle"">{HttpUtility.HtmlEncode(value)}</text>
    <text class=""stat-label {cssClass}"" x=""{x}"" y=""{labelY:F1}"" text-anchor=""middle"">{HttpUtility.HtmlEncode(label)}</text>
    <text class=""stat-date"" x=""{x}"" y=""{dateY:F1}"" text-anchor=""middle"">{HttpUtility.HtmlEncode(dateRange)}</text>
</g>";
    }

    private static string RenderCurrentStreakSectionFixed(int x, double fireY, double ringY, double numberY, double labelY, double dateY, string value, string label, string dateRange, string fireColor, string ringColor, int animationDelay)
    {
        // Fire icon - simple flame SVG (positioned at top center of ring)
        var fireIcon = $@"<svg x=""{x - 10}"" y=""{fireY:F1}"" width=""20"" height=""25"" viewBox=""0 0 24 24"" fill=""#{fireColor}"">
            <path d=""M12 23C16.1421 23 19.5 19.6421 19.5 15.5C19.5 14.1284 19.1526 12.8394 18.5389 11.7131C17.8634 10.4756 16.8806 9.28395 15.7895 8.17157C15.4043 7.78457 14.9377 7.38 14.4297 6.95C13.3553 6.04395 12.0833 5 11.2929 3.46447L10.5 2L9.70711 3.46447C8.91667 5 7.64474 6.04395 6.57034 6.95C6.06229 7.38 5.59575 7.78457 5.21053 8.17157C4.1194 9.28395 3.13663 10.4756 2.46106 11.7131C1.84737 12.8394 1.5 14.1284 1.5 15.5C1.5 19.6421 4.85786 23 9 23H12Z""/>
        </svg>";

        // Ring around the current streak number
        var ring = $@"<circle cx=""{x}"" cy=""{ringY:F1}"" r=""40"" fill=""none"" stroke=""#{ringColor}"" stroke-width=""5"" class=""ring""/>";

        return $@"
<g class=""streak-section"" style=""animation-delay: {animationDelay}ms"">
    {fireIcon}
    {ring}
    <text class=""stat-value current"" x=""{x}"" y=""{numberY:F1}"" text-anchor=""middle"">{HttpUtility.HtmlEncode(value)}</text>
    <text class=""stat-label current"" x=""{x}"" y=""{labelY:F1}"" text-anchor=""middle"">{HttpUtility.HtmlEncode(label)}</text>
    <text class=""stat-date"" x=""{x}"" y=""{dateY:F1}"" text-anchor=""middle"">{HttpUtility.HtmlEncode(dateRange)}</text>
</g>";
    }

    private static string FormatDateRange(DateOnly? start, DateOnly? end)
    {
        if (!start.HasValue || !end.HasValue)
            return "No Data";

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        string FormatDate(DateOnly date)
        {
            if (date == today)
                return "Present";

            // Format: "Jan 5" or "Jan 5, 2023" if different year
            if (date.Year == today.Year)
                return date.ToString("MMM d");
            return date.ToString("MMM d, yyyy");
        }

        var startStr = FormatDate(start.Value);
        var endStr = FormatDate(end.Value);

        if (start.Value == end.Value)
            return startStr;

        return $"{startStr} - {endStr}";
    }

    #endregion
}
