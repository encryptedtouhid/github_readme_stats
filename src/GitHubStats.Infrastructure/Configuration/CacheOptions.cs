namespace GitHubStats.Infrastructure.Configuration;

/// <summary>
/// Configuration options for caching.
/// </summary>
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Redis connection string. If null, uses in-memory cache.
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Instance name prefix for Redis keys.
    /// </summary>
    public string InstanceName { get; set; } = "GitHubStats:";

    /// <summary>
    /// Default cache duration for stats cards in seconds.
    /// </summary>
    public int StatsCardTtlSeconds { get; set; } = 86400; // 1 day

    /// <summary>
    /// Default cache duration for top languages cards in seconds.
    /// </summary>
    public int TopLangsCardTtlSeconds { get; set; } = 518400; // 6 days

    /// <summary>
    /// Default cache duration for pin cards in seconds.
    /// </summary>
    public int PinCardTtlSeconds { get; set; } = 864000; // 10 days

    /// <summary>
    /// Default cache duration for gist cards in seconds.
    /// </summary>
    public int GistCardTtlSeconds { get; set; } = 172800; // 2 days

    /// <summary>
    /// Default cache duration for WakaTime cards in seconds.
    /// </summary>
    public int WakaTimeCardTtlSeconds { get; set; } = 86400; // 1 day

    /// <summary>
    /// Cache duration for error responses in seconds.
    /// </summary>
    public int ErrorTtlSeconds { get; set; } = 600; // 10 minutes
}
