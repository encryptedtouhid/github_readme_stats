using System.Threading.RateLimiting;
using GitHubStats.Api.Endpoints;
using GitHubStats.Application.Extensions;
using GitHubStats.Infrastructure.Configuration;
using GitHubStats.Infrastructure.Extensions;
using GitHubStats.Rendering.Extensions;
using HealthChecks.Redis;
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

if (!string.IsNullOrEmpty(cacheOptions.RedisConnectionString))
{
    builder.Services.AddStackExchangeRedisOutputCache(options =>
    {
        options.Configuration = cacheOptions.RedisConnectionString;
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
        policy.Expire(TimeSpan.FromHours(12))
              .Tag("stats"));

    // Top languages card cache policy
    options.AddPolicy("TopLangsCard", policy =>
        policy.Expire(TimeSpan.FromDays(6))
              .Tag("langs"));

    // Repo pin card cache policy
    options.AddPolicy("RepoCard", policy =>
        policy.Expire(TimeSpan.FromDays(10))
              .Tag("repo"));

    // Gist card cache policy
    options.AddPolicy("GistCard", policy =>
        policy.Expire(TimeSpan.FromDays(2))
              .Tag("gist"));

    // WakaTime card cache policy
    options.AddPolicy("WakaTimeCard", policy =>
        policy.Expire(TimeSpan.FromHours(12))
              .Tag("wakatime"));

    // Streak card cache policy
    options.AddPolicy("StreakCard", policy =>
        policy.Expire(TimeSpan.FromHours(3))
              .Tag("streak"));
});

// Health Checks
builder.Services.AddHealthChecks();

if (!string.IsNullOrEmpty(cacheOptions.RedisConnectionString))
{
    builder.Services.AddHealthChecks()
        .AddRedis(cacheOptions.RedisConnectionString, name: "redis");
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
app.UseResponseCompression();
app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseOutputCache();

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
app.MapWakaTimeEndpoint();
app.MapStreakEndpoint();

// Status endpoints
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "GitHub Readme Stats" }))
   .WithName("Root")
   .WithTags("Status");

app.MapGet("/api/status/up", () => Results.Ok(new { status = "up", timestamp = DateTime.UtcNow }))
   .WithName("StatusUp")
   .WithTags("Status");

app.Run();
