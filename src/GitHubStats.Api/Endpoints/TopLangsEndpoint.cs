using GitHubStats.Application.Services;
using GitHubStats.Domain.Exceptions;
using GitHubStats.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace GitHubStats.Api.Endpoints;

public static class TopLangsEndpoint
{
    public static void MapTopLangsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/top-langs", async (
            [FromQuery] string? username,
            [FromQuery] string? hide,
            [FromQuery] string? layout,
            [FromQuery(Name = "langs_count")] int? langsCount,
            [FromQuery(Name = "exclude_repo")] string? excludeRepo,
            [FromQuery(Name = "size_weight")] double? sizeWeight,
            [FromQuery(Name = "count_weight")] double? countWeight,
            [FromQuery(Name = "hide_progress")] bool? hideProgress,
            [FromQuery(Name = "hide_title")] bool? hideTitle,
            [FromQuery(Name = "hide_border")] bool? hideBorder,
            [FromQuery(Name = "card_width")] int? cardWidth,
            [FromQuery] string? theme,
            [FromQuery(Name = "title_color")] string? titleColor,
            [FromQuery(Name = "text_color")] string? textColor,
            [FromQuery(Name = "bg_color")] string? bgColor,
            [FromQuery(Name = "border_color")] string? borderColor,
            [FromQuery(Name = "border_radius")] double? borderRadius,
            [FromQuery(Name = "cache_seconds")] int? cacheSeconds,
            [FromQuery] string? locale,
            [FromQuery(Name = "disable_animations")] bool? disableAnimations,
            [FromQuery(Name = "custom_title")] string? customTitle,
            [FromQuery(Name = "stats_format")] string? statsFormat,
            TopLanguagesCardService service,
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

            // Validate layout
            var validLayouts = new[] { "normal", "compact", "donut", "donut-vertical", "pie" };
            if (layout != null && !validLayouts.Contains(layout.ToLowerInvariant()))
            {
                context.Response.ContentType = "image/svg+xml";
                return Results.Content(
                    renderer.RenderErrorCard($"Invalid layout: {layout}. Valid options: {string.Join(", ", validLayouts)}"),
                    "image/svg+xml");
            }

            // Validate stats_format
            var validFormats = new[] { "percentages", "bytes" };
            if (statsFormat != null && !validFormats.Contains(statsFormat.ToLowerInvariant()))
            {
                context.Response.ContentType = "image/svg+xml";
                return Results.Content(
                    renderer.RenderErrorCard($"Invalid stats_format: {statsFormat}. Valid options: {string.Join(", ", validFormats)}"),
                    "image/svg+xml");
            }

            try
            {
                var options = new TopLanguagesCardOptions
                {
                    Theme = theme,
                    TitleColor = titleColor,
                    TextColor = textColor,
                    BgColor = bgColor,
                    BorderColor = borderColor,
                    BorderRadius = borderRadius,
                    HideBorder = hideBorder ?? false,
                    HideTitle = hideTitle ?? false,
                    CustomTitle = customTitle,
                    Locale = locale,
                    DisableAnimations = disableAnimations ?? false,
                    Hide = hide?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Layout = layout ?? "normal",
                    LangsCount = langsCount,
                    CardWidth = cardWidth,
                    HideProgress = hideProgress ?? false,
                    StatsFormat = statsFormat ?? "percentages"
                };

                var excludeRepos = excludeRepo?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                var svg = await service.GenerateCardAsync(
                    username,
                    options,
                    excludeRepos,
                    sizeWeight ?? 1,
                    countWeight ?? 0,
                    cacheSeconds.HasValue ? TimeSpan.FromSeconds(cacheSeconds.Value) : null,
                    cancellationToken);

                SetCacheHeaders(context, cacheSeconds ?? 518400);

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
        .WithName("GetTopLangs")
        .WithTags("Languages")
        .RequireRateLimiting("perIp")
        .CacheOutput("TopLangsCard");
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
