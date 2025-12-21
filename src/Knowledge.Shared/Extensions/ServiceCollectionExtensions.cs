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
        // Register KnowledgeSettings from configuration (appsettings.json, user secrets, etc.)
        var knowledgeSettings = new KnowledgeSettings();
        configuration.GetSection(KnowledgeSettings.SectionName).Bind(knowledgeSettings);
        services.AddSingleton(knowledgeSettings);

        return services;
    }
}
