using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Knowledge.Shared.Configuration;

namespace Knowledge.Shared.Extensions;

/// <summary>
/// Extension methods for configuring shared services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds shared services to the service collection.
    /// </summary>
    public static IServiceCollection AddSharedServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KnowledgeSettings>(configuration.GetSection(KnowledgeSettings.SectionName));

        return services;
    }
}
