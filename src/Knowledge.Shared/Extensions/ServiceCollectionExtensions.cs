using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Knowledge.Shared.Abstractions;
using Knowledge.Shared.Configuration;
using Knowledge.Shared.Data;
using Knowledge.Shared.Services;

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

    /// <summary>
    /// Adds the embedding service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="embeddingGenerator">The embedding generator to use.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEmbeddingService(
        this IServiceCollection services,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        services.AddSingleton(embeddingGenerator);
        services.AddSingleton<IEmbeddingService, EmbeddingService>();

        return services;
    }

    /// <summary>
    /// Adds the KnowledgeDbContext factory with PostgreSQL and pgvector support.
    /// Uses IDbContextFactory for proper lifetime management outside of request scope.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration containing the connection string.</param>
    /// <param name="connectionStringName">The name of the connection string (default: "Postgres").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKnowledgeDbContext(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "Postgres")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");

        services.AddDbContextFactory<KnowledgeDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Enable pgvector support
                npgsqlOptions.UseVector();

                // Enable retry on transient failures
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        return services;
    }
}
