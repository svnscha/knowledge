using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Knowledge.Shared.Configuration;
using Knowledge.Shared.Logging;

namespace Knowledge.Shared.Extensions;

/// <summary>
/// Extension methods for bootstrapping ASP.NET Core applications with shared configuration.
/// Makes your Program.cs as clean as a freshly formatted SSD.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the WebApplicationBuilder with all shared services and configuration.
    /// Call this once and you're ready to build your agents.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder to configure.</param>
    /// <param name="configureServices">Callback to configure custom services.</param>
    /// <param name="apiTitle">Title for the Swagger documentation.</param>
    /// <param name="apiDescription">Description for the Swagger documentation.</param>
    /// <returns>A configured WebApplicationBuilder ready for additional customizations.</returns>
    public static WebApplicationBuilder ConfigureKnowledgeDefaults(
        this WebApplicationBuilder builder,
        Action<KnowledgeSettings> configureServices,
        string apiTitle = "Knowledge",
        string apiDescription = "Knowledge with AI capabilities")
    {
        // Create bootstrap logger for startup messages
        using var bootstrapLoggerFactory = LoggerFactory.Create(logging =>
        {
            logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
            });
        });
        var bootstrapLogger = bootstrapLoggerFactory.CreateLogger("Knowledge");

        bootstrapLogger.LogInformation("Initializing...");

        // Add shared services (registers KnowledgeSettings)
        builder.Services.AddSharedServices(builder.Configuration);

        // Get KnowledgeSettings for configuration callback
        var knowledgeSettings = new KnowledgeSettings();
        builder.Configuration.GetSection(KnowledgeSettings.SectionName).Bind(knowledgeSettings);

        bootstrapLogger.LogInformation("Application: {AppName}, LogLevel: {LogLevel}", knowledgeSettings.Name, knowledgeSettings.LogLevel);

        bootstrapLogger.LogInformation("Configuring services...");

        // Add API endpoint explorer and Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = apiTitle,
                Version = "v1",
                Description = apiDescription
            });
        });

        // Configure logging with shared configuration
        builder.Logging.ConfigureSharedLogging();

        configureServices(knowledgeSettings);

        return builder;
    }

    /// <summary>
    /// Configures the HTTP request pipeline with shared middleware.
    /// </summary>
    /// <param name="app">The WebApplication to configure.</param>
    /// <returns>The configured WebApplication.</returns>
    public static WebApplication ConfigureKnowledgePipeline(this WebApplication app)
    {
        // Get KnowledgeSettings from DI
        var knowledgeSettings = app.Services.GetRequiredService<KnowledgeSettings>();

        // Enable Swagger in development
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Knowledge API v1");
                options.RoutePrefix = "swagger";
            });
        }

        // Only redirect to HTTPS if enabled (from KnowledgeSettings)
        if (knowledgeSettings.HttpsRedirectEnabled)
        {
            app.UseHttpsRedirection();
        }

        return app;
    }

    /// <summary>
    /// Logs a welcome message with available endpoints after the application has started.
    /// Call this after app.Run() setup but before actually running.
    /// </summary>
    /// <param name="app">The WebApplication instance.</param>
    /// <returns>The WebApplication for chaining.</returns>
    public static WebApplication LogStartupComplete(this WebApplication app)
    {
        var knowledgeSettings = app.Services.GetRequiredService<KnowledgeSettings>();
        var logger = app.Services.GetRequiredService<ILogger<KnowledgeSettings>>();
        var baseUrl = knowledgeSettings.BaseUrl.TrimEnd('/');

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            logger.LogInformation("═══════════════════════════════════════════════════════════");
            logger.LogInformation("  {AppName} is ready!", knowledgeSettings.Name);
            logger.LogInformation("═══════════════════════════════════════════════════════════");
            logger.LogInformation("  Available endpoints:");
            logger.LogInformation("    • Home:    {BaseUrl}/", baseUrl);
            logger.LogInformation("    • DevUI:   {BaseUrl}/devui", baseUrl);
            if (app.Environment.IsDevelopment())
            {
                logger.LogInformation("    • Swagger: {BaseUrl}/swagger", baseUrl);
            }
            logger.LogInformation("═══════════════════════════════════════════════════════════");
        });

        return app;
    }
}
