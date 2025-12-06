using GitHubStats.Application.Services;
using GitHubStats.Domain.Exceptions;
using GitHubStats.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace GitHubStats.Api.Endpoints;

public static class StatsEndpoint
{
    public static void MapStatsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stats", async (
            [FromQuery] string? username,
            [FromQuery] string? hide,
            [FromQuery] string? show,
            [FromQuery(Name = "hide_title")] bool? hideTitle,
            [FromQuery(Name = "hide_border")] bool? hideBorder,
            [FromQuery(Name = "hide_rank")] bool? hideRank,
            [FromQuery(Name = "show_icons")] bool? showIcons,
            [FromQuery(Name = "include_all_commits")] bool? includeAllCommits,
            [FromQuery(Name = "commits_year")] int? commitsYear,
            [FromQuery] string? theme,
            [FromQuery(Name = "title_color")] string? titleColor,
            [FromQuery(Name = "text_color")] string? textColor,
            [FromQuery(Name = "icon_color")] string? iconColor,
            [FromQuery(Name = "bg_color")] string? bgColor,
            [FromQuery(Name = "border_color")] string? borderColor,
            [FromQuery(Name = "border_radius")] double? borderRadius,
            [FromQuery(Name = "ring_color")] string? ringColor,
            [FromQuery(Name = "cache_seconds")] int? cacheSeconds,
            [FromQuery] string? locale,
            [FromQuery(Name = "disable_animations")] bool? disableAnimations,
            [FromQuery(Name = "rank_icon")] string? rankIcon,
            [FromQuery(Name = "number_format")] string? numberFormat,
            [FromQuery(Name = "text_bold")] bool? textBold,
            [FromQuery(Name = "exclude_repo")] string? excludeRepo,
            [FromQuery(Name = "line_height")] int? lineHeight,
            [FromQuery(Name = "card_width")] int? cardWidth,
            StatsCardService service,
            ICardRenderer renderer,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                context.Response.ContentType = "image/svg+xml";
                return Results.Content(
                    renderer.RenderErrorCard("Missing required parameter: username"),
                    "image/svg+xml");
            }

            try
            {
                var options = new StatsCardOptions
                {
                    Theme = theme,
                    TitleColor = titleColor,
                    TextColor = textColor,
                    IconColor = iconColor,
                    BgColor = bgColor,
                    BorderColor = borderColor,
                    BorderRadius = borderRadius,
                    HideBorder = hideBorder ?? false,
                    HideTitle = hideTitle ?? false,
                    Locale = locale,
                    DisableAnimations = disableAnimations ?? false,
                    Hide = hide?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Show = show?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    ShowIcons = showIcons ?? false,
                    HideRank = hideRank ?? false,
                    IncludeAllCommits = includeAllCommits ?? false,
                    CommitsYear = commitsYear,
                    LineHeight = lineHeight,
                    CardWidth = cardWidth,
                    RingColor = ringColor,
                    TextBold = textBold ?? true,
                    NumberFormat = numberFormat ?? "short",
                    RankIcon = rankIcon ?? "default"
                };

                var excludeRepos = excludeRepo?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                var svg = await service.GenerateCardAsync(
                    username,
                    options,
                    includeAllCommits ?? false,
                    excludeRepos,
                    show?.Contains("prs_merged") ?? false,
                    show?.Contains("discussions_started") ?? false,
                    show?.Contains("discussions_answered") ?? false,
                    commitsYear,
                    cacheSeconds.HasValue ? TimeSpan.FromSeconds(cacheSeconds.Value) : null,
                    cancellationToken);

                // Set cache headers
                SetCacheHeaders(context, cacheSeconds ?? 1800);

                context.Response.ContentType = "image/svg+xml";
                return Results.Content(svg, "image/svg+xml");
            }
            catch (DomainException ex)
            {
                SetErrorCacheHeaders(context);
                context.Response.ContentType = "image/svg+xml";
                return Results.Content(
                    renderer.RenderErrorCard(ex.Message),
                    "image/svg+xml");
            }
        })
        .WithName("GetStats")
        .WithTags("Stats")
        .RequireRateLimiting("stats")
        .CacheOutput("StatsCard");
    }

    private static void SetCacheHeaders(HttpContext context, int seconds)
    {
        context.Response.Headers.CacheControl = $"max-age={seconds}, s-maxage={seconds}, stale-while-revalidate=86400";
    }

    private static void SetErrorCacheHeaders(HttpContext context)
    {
        context.Response.Headers.CacheControl = "max-age=600, s-maxage=600, stale-while-revalidate=86400";
    }
}
