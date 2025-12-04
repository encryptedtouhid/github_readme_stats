using GitHubStats.Application.Services;
using GitHubStats.Domain.Exceptions;
using GitHubStats.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace GitHubStats.Api.Endpoints;

public static class WakaTimeEndpoint
{
    public static void MapWakaTimeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/wakatime", async (
            [FromQuery] string? username,
            [FromQuery] string? hide,
            [FromQuery] string? layout,
            [FromQuery(Name = "langs_count")] int? langsCount,
            [FromQuery(Name = "api_domain")] string? apiDomain,
            [FromQuery(Name = "hide_progress")] bool? hideProgress,
            [FromQuery(Name = "hide_title")] bool? hideTitle,
            [FromQuery(Name = "hide_border")] bool? hideBorder,
            [FromQuery(Name = "card_width")] int? cardWidth,
            [FromQuery(Name = "line_height")] int? lineHeight,
            [FromQuery] string? theme,
            [FromQuery(Name = "title_color")] string? titleColor,
            [FromQuery(Name = "text_color")] string? textColor,
            [FromQuery(Name = "icon_color")] string? iconColor,
            [FromQuery(Name = "bg_color")] string? bgColor,
            [FromQuery(Name = "border_color")] string? borderColor,
            [FromQuery(Name = "border_radius")] double? borderRadius,
            [FromQuery(Name = "cache_seconds")] int? cacheSeconds,
            [FromQuery] string? locale,
            [FromQuery(Name = "disable_animations")] bool? disableAnimations,
            [FromQuery(Name = "custom_title")] string? customTitle,
            [FromQuery(Name = "display_format")] string? displayFormat,
            WakaTimeCardService service,
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
            var validLayouts = new[] { "default", "compact" };
            if (layout != null && !validLayouts.Contains(layout.ToLowerInvariant()))
            {
                context.Response.ContentType = "image/svg+xml";
                return Results.Content(
                    renderer.RenderErrorCard($"Invalid layout: {layout}. Valid options: {string.Join(", ", validLayouts)}"),
                    "image/svg+xml");
            }

            // Validate display_format
            var validFormats = new[] { "time", "percent" };
            if (displayFormat != null && !validFormats.Contains(displayFormat.ToLowerInvariant()))
            {
                context.Response.ContentType = "image/svg+xml";
                return Results.Content(
                    renderer.RenderErrorCard($"Invalid display_format: {displayFormat}. Valid options: {string.Join(", ", validFormats)}"),
                    "image/svg+xml");
            }

            try
            {
                var options = new WakaTimeCardOptions
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
                    CustomTitle = customTitle,
                    Locale = locale,
                    DisableAnimations = disableAnimations ?? false,
                    Hide = hide?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Layout = layout ?? "default",
                    LangsCount = langsCount,
                    CardWidth = cardWidth,
                    LineHeight = lineHeight,
                    HideProgress = hideProgress ?? false,
                    DisplayFormat = displayFormat ?? "time"
                };

                var svg = await service.GenerateCardAsync(
                    username,
                    options,
                    apiDomain,
                    cacheSeconds.HasValue ? TimeSpan.FromSeconds(cacheSeconds.Value) : null,
                    cancellationToken);

                SetCacheHeaders(context, cacheSeconds ?? 86400);

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
        .WithName("GetWakaTime")
        .WithTags("WakaTime")
        .RequireRateLimiting("perIp")
        .CacheOutput("WakaTimeCard");
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
