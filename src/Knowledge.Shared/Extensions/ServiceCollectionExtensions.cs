using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Knowledge.Shared.Configuration;

namespace Knowledge.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds shared services to the service collection.
    /// </summary>
    public static IServiceCollection AddSharedServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register KnowledgeSettings from environment variables
        var knowledgeSettings = KnowledgeSettingsExtensions.FromEnvironment();
        services.AddSingleton(knowledgeSettings);

        return services;
    }
}
