using GitHubStats.Application.Services;
using GitHubStats.Domain.Exceptions;
using GitHubStats.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace GitHubStats.Api.Endpoints;

public static class GistEndpoint
{
    public static void MapGistEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/gist", async (
            [FromQuery] string? id,
            [FromQuery(Name = "hide_border")] bool? hideBorder,
            [FromQuery(Name = "hide_title")] bool? hideTitle,
            [FromQuery(Name = "show_owner")] bool? showOwner,
            [FromQuery] string? theme,
            [FromQuery(Name = "title_color")] string? titleColor,
            [FromQuery(Name = "text_color")] string? textColor,
            [FromQuery(Name = "icon_color")] string? iconColor,
            [FromQuery(Name = "bg_color")] string? bgColor,
            [FromQuery(Name = "border_color")] string? borderColor,
            [FromQuery(Name = "border_radius")] double? borderRadius,
            [FromQuery(Name = "cache_seconds")] int? cacheSeconds,
            [FromQuery] string? locale,
            GistCardService service,
            ICardRenderer renderer,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                context.Response.ContentType = "image/svg+xml";
                return Results.Content(
                    renderer.RenderErrorCard("Missing required parameter: id"),
                    "image/svg+xml");
            }

            try
            {
                var options = new GistCardOptions
                {
                    Theme = theme ?? "default_repocard",
                    TitleColor = titleColor,
                    TextColor = textColor,
                    IconColor = iconColor,
                    BgColor = bgColor,
                    BorderColor = borderColor,
                    BorderRadius = borderRadius,
                    HideBorder = hideBorder ?? false,
                    HideTitle = hideTitle ?? false,
                    Locale = locale,
                    ShowOwner = showOwner ?? false
                };

                var svg = await service.GenerateCardAsync(
                    id,
                    options,
                    cacheSeconds.HasValue ? TimeSpan.FromSeconds(cacheSeconds.Value) : null,
                    cancellationToken);

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
        .WithName("GetGist")
        .WithTags("Gist")
        .RequireRateLimiting("perIp")
        .CacheOutput("GistCard");
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
