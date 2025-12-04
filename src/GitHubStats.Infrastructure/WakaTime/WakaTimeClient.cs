using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Exceptions;
using GitHubStats.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GitHubStats.Infrastructure.WakaTime;

/// <summary>
/// WakaTime API client implementation.
/// </summary>
public sealed class WakaTimeClient : IWakaTimeClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WakaTimeClient> _logger;
    private const string DefaultApiDomain = "wakatime.com";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public WakaTimeClient(HttpClient httpClient, ILogger<WakaTimeClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WakaTimeStats> GetStatsAsync(
        string username,
        string? apiDomain = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new MissingParameterException(["username"]);
        }

        var domain = string.IsNullOrWhiteSpace(apiDomain)
            ? DefaultApiDomain
            : apiDomain.TrimEnd('/');

        var url = $"https://{domain}/api/v1/users/{username}/stats?is_including_today=true";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new DomainException(
                    $"Could not resolve to a User with the login of '{username}'",
                    "WAKATIME_USER_NOT_FOUND");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<WakaTimeApiResponse>(content, JsonOptions);

            if (result?.Data == null)
            {
                throw new DomainException("Invalid WakaTime response", "WAKATIME_ERROR");
            }

            var data = result.Data;
            return new WakaTimeStats
            {
                Languages = data.Languages?.Select(l => new WakaTimeLanguage
                {
                    Name = l.Name,
                    Percent = l.Percent,
                    Hours = l.Hours,
                    Minutes = l.Minutes,
                    Text = l.Text
                }).ToList() ?? [],
                Range = data.Range,
                IsCodingActivityVisible = data.IsCodingActivityVisible,
                IsOtherUsageVisible = data.IsOtherUsageVisible
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch WakaTime stats for user: {Username}", username);
            throw new DomainException(
                $"Could not resolve to a User with the login of '{username}'",
                "WAKATIME_USER_NOT_FOUND",
                ex);
        }
    }
}

#region Response DTOs

internal sealed class WakaTimeApiResponse
{
    public WakaTimeApiData? Data { get; set; }
}

internal sealed class WakaTimeApiData
{
    public List<WakaTimeLanguageDto>? Languages { get; set; }
    public string? Range { get; set; }
    [JsonPropertyName("is_coding_activity_visible")]
    public bool IsCodingActivityVisible { get; set; }
    [JsonPropertyName("is_other_usage_visible")]
    public bool IsOtherUsageVisible { get; set; }
}

internal sealed class WakaTimeLanguageDto
{
    public required string Name { get; set; }
    public double Percent { get; set; }
    public int Hours { get; set; }
    public int Minutes { get; set; }
    public string? Text { get; set; }
}

#endregion
