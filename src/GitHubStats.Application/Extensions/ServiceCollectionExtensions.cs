using GitHubStats.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GitHubStats.Application.Extensions;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application layer services.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<StatsCardService>();
        services.AddScoped<RepoCardService>();
        services.AddScoped<TopLanguagesCardService>();
        services.AddScoped<GistCardService>();
        services.AddScoped<StreakCardService>();

        return services;
    }
}
