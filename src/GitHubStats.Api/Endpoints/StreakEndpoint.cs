using GitHubStats.Application.Services;
using GitHubStats.Domain.Exceptions;
using GitHubStats.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace GitHubStats.Api.Endpoints;

public static class StreakEndpoint
{
    public static void MapStreakEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/streak", async (
            [FromQuery] string? username,
            [FromQuery(Name = "hide_border")] bool? hideBorder,
            [FromQuery] string? theme,
            [FromQuery(Name = "title_color")] string? titleColor,
            [FromQuery(Name = "text_color")] string? textColor,
            [FromQuery(Name = "icon_color")] string? iconColor,
            [FromQuery(Name = "bg_color")] string? bgColor,
            [FromQuery(Name = "border_color")] string? borderColor,
            [FromQuery(Name = "border_radius")] double? borderRadius,
            [FromQuery(Name = "ring_color")] string? ringColor,
            [FromQuery(Name = "fire_color")] string? fireColor,
            [FromQuery(Name = "stroke_color")] string? strokeColor,
            [FromQuery(Name = "curr_streak_num_color")] string? currStreakNumColor,
            [FromQuery(Name = "side_nums_color")] string? sideNumsColor,
            [FromQuery(Name = "curr_streak_label_color")] string? currStreakLabelColor,
            [FromQuery(Name = "side_labels_color")] string? sideLabelsColor,
            [FromQuery(Name = "dates_color")] string? datesColor,
            [FromQuery(Name = "date_format")] string? dateFormat,
            [FromQuery(Name = "card_width")] int? cardWidth,
            [FromQuery(Name = "card_height")] int? cardHeight,
            [FromQuery(Name = "hide_total_contributions")] bool? hideTotalContributions,
            [FromQuery(Name = "hide_current_streak")] bool? hideCurrentStreak,
            [FromQuery(Name = "hide_longest_streak")] bool? hideLongestStreak,
            [FromQuery(Name = "starting_year")] int? startingYear,
            [FromQuery(Name = "cache_seconds")] int? cacheSeconds,
            [FromQuery] string? locale,
            [FromQuery(Name = "disable_animations")] bool? disableAnimations,
            StreakCardService service,
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
                var options = new StreakCardOptions
                {
                    Theme = theme,
                    TitleColor = titleColor,
                    TextColor = textColor,
                    IconColor = iconColor,
                    BgColor = bgColor,
                    BorderColor = borderColor,
                    BorderRadius = borderRadius,
                    HideBorder = hideBorder ?? false,
                    Locale = locale,
                    DisableAnimations = disableAnimations ?? false,
                    RingColor = ringColor,
                    FireColor = fireColor,
                    StrokeColor = strokeColor,
                    CurrStreakNumColor = currStreakNumColor,
                    SideNumsColor = sideNumsColor,
                    CurrStreakLabelColor = currStreakLabelColor,
                    SideLabelsColor = sideLabelsColor,
                    DatesColor = datesColor,
                    DateFormat = dateFormat ?? "M j[, Y]",
                    CardWidth = cardWidth,
                    CardHeight = cardHeight,
                    HideTotalContributions = hideTotalContributions ?? false,
                    HideCurrentStreak = hideCurrentStreak ?? false,
                    HideLongestStreak = hideLongestStreak ?? false,
                    StartingYear = startingYear
                };

                var svg = await service.GenerateCardAsync(
                    username,
                    options,
                    startingYear,
                    cacheSeconds.HasValue ? TimeSpan.FromSeconds(cacheSeconds.Value) : null,
                    cancellationToken);

                // Set cache headers (3 hours default like original)
                SetCacheHeaders(context, cacheSeconds ?? 10800);

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
        .WithName("GetStreak")
        .WithTags("Streak")
        .RequireRateLimiting("perIp")
        .CacheOutput("StreakCard");
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
