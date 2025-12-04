namespace GitHubStats.Domain.Entities;

/// <summary>
/// Represents WakaTime coding statistics.
/// </summary>
public sealed record WakaTimeStats
{
    public required IReadOnlyList<WakaTimeLanguage> Languages { get; init; }
    public string? Range { get; init; }
    public bool IsCodingActivityVisible { get; init; }
    public bool IsOtherUsageVisible { get; init; }
}

/// <summary>
/// Represents language usage in WakaTime.
/// </summary>
public sealed record WakaTimeLanguage
{
    public required string Name { get; init; }
    public double Percent { get; init; }
    public int Hours { get; init; }
    public int Minutes { get; init; }
    public string? Text { get; init; }
}
