using GitHubStats.Domain.Interfaces;
using GitHubStats.Rendering.Cards;
using Microsoft.Extensions.DependencyInjection;

namespace GitHubStats.Rendering.Extensions;

/// <summary>
/// Extension methods for configuring rendering services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds rendering services.
    /// </summary>
    public static IServiceCollection AddRendering(this IServiceCollection services)
    {
        services.AddSingleton<ICardRenderer, CardRenderer>();
        return services;
    }
}
