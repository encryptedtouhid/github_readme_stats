using System.Threading.RateLimiting;
using GitHubStats.Api.Endpoints;
using GitHubStats.Application.Extensions;
using GitHubStats.Infrastructure.Configuration;
using GitHubStats.Infrastructure.Extensions;
using GitHubStats.Rendering.Extensions;
using HealthChecks.Redis;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// OpenTelemetry for observability
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("GitHubStats"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

// Rate Limiting - Critical for handling millions of users
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global rate limit: 10,000 requests per minute
    options.AddFixedWindowLimiter("global", limiter =>
    {
        limiter.PermitLimit = 10000;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 100;
    });

    // Per-IP rate limit: 100 requests per minute
    options.AddPolicy("perIp", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            });
    });

    // Stricter limit for stats endpoint (expensive operations)
    options.AddPolicy("stats", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            });
    });
});

// Output Caching with Redis for distributed caching
var cacheOptions = builder.Configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();
var redisConnString = cacheOptions.GetStackExchangeConnectionString();

if (!string.IsNullOrEmpty(redisConnString))
{
    builder.Services.AddStackExchangeRedisOutputCache(options =>
    {
        options.Configuration = redisConnString;
        options.InstanceName = cacheOptions.InstanceName;
    });
}
else
{
    builder.Services.AddOutputCache();
}

builder.Services.AddOutputCache(options =>
{
    // Stats card cache policy
    options.AddPolicy("StatsCard", policy =>
        policy.Expire(TimeSpan.FromMinutes(30))
              .SetVaryByQuery("username", "theme", "show_icons", "hide_title", "hide_border", "hide_rank", "hide", "show", "include_all_commits", "commits_year", "title_color", "text_color", "icon_color", "bg_color", "border_color", "border_radius", "ring_color", "cache_seconds", "locale", "disable_animations", "rank_icon", "number_format", "text_bold", "exclude_repo", "line_height", "card_width")
              .Tag("stats"));

    // Streak card cache policy
    options.AddPolicy("StreakCard", policy =>
        policy.Expire(TimeSpan.FromMinutes(30))
              .SetVaryByQuery("username", "theme", "hide_border", "border_radius", "title_color", "text_color", "icon_color", "bg_color", "border_color", "ring_color", "fire_color", "stroke_color", "curr_streak_num_color", "side_nums_color", "curr_streak_label_color", "side_labels_color", "dates_color", "date_format", "card_width", "card_height", "hide_total_contributions", "hide_current_streak", "hide_longest_streak", "starting_year", "cache_seconds", "locale", "disable_animations")
              .Tag("streak"));

    // Top languages card cache policy
    options.AddPolicy("TopLangsCard", policy =>
        policy.Expire(TimeSpan.FromMinutes(30))
              .SetVaryByQuery("username", "theme", "hide", "layout", "langs_count", "exclude_repo", "size_weight", "count_weight", "hide_progress", "hide_title", "hide_border", "card_width", "title_color", "text_color", "bg_color", "border_color", "border_radius", "cache_seconds", "locale", "disable_animations", "custom_title", "stats_format")
              .Tag("top-langs"));

    // Repo pin card cache policy (used by RepoEndpoint)
    options.AddPolicy("RepoCard", policy =>
        policy.Expire(TimeSpan.FromMinutes(30))
              .Tag("repo"));

    // Gist card cache policy (used by GistEndpoint)
    options.AddPolicy("GistCard", policy =>
        policy.Expire(TimeSpan.FromMinutes(30))
              .Tag("gist"));
});

// Health Checks
builder.Services.AddHealthChecks();

if (!string.IsNullOrEmpty(redisConnString))
{
    builder.Services.AddHealthChecks()
        .AddRedis(redisConnString, name: "redis");
}

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// CORS for embedding in GitHub READMEs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Infrastructure services (GitHub client, caching, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Application services
builder.Services.AddApplication();

// Add Rendering services
builder.Services.AddRendering();

// Problem Details for consistent error responses
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline
// Behind a reverse proxy (Caddy/Cloudflare) in Docker: trust X-Forwarded-* so
// per-IP rate limiting sees the real client address.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseResponseCompression();
app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseOutputCache();
app.UseDefaultFiles();
app.UseStaticFiles();

// Exception handling middleware
app.UseExceptionHandler();
app.UseStatusCodePages();

// Health check endpoint
app.MapHealthChecks("/health");

// API Endpoints
app.MapStatsEndpoint();
app.MapRepoEndpoint();
app.MapTopLangsEndpoint();
app.MapGistEndpoint();
app.MapStreakEndpoint();
app.MapProgressEndpoint();

// Status endpoints
// Root is now served by wwwroot/index.html via UseDefaultFiles + UseStaticFiles

app.MapGet("/api/status/up", () => Results.Ok(new { status = "up", timestamp = DateTime.UtcNow }))
   .WithName("StatusUp")
   .WithTags("Status");

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
