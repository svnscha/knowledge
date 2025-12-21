using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Knowledge.Shared.Configuration;
using Knowledge.Shared.Logging;

namespace Knowledge.Shared.Extensions;

/// <summary>
/// Extension methods for bootstrapping ASP.NET Core applications.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the WebApplicationBuilder with shared services and configuration.
    /// </summary>
    public static WebApplicationBuilder ConfigureKnowledgeDefaults(
        this WebApplicationBuilder builder,
        Action<KnowledgeSettings, ILogger> configureServices,
        string apiTitle = "Knowledge",
        string apiDescription = "Knowledge with AI capabilities")
    {
        // Bootstrap logger for startup messages before DI is ready
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

        // Register IOptions<KnowledgeSettings>
        builder.Services.AddSharedServices(builder.Configuration);

        // Bind settings for callback (before DI container exists)
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

        configureServices(knowledgeSettings, bootstrapLogger);

        return builder;
    }

    /// <summary>
    /// Configures the HTTP request pipeline with shared middleware.
    /// </summary>
    public static WebApplication ConfigureKnowledgePipeline(this WebApplication app)
    {
        var knowledgeSettings = app.Services.GetRequiredService<IOptions<KnowledgeSettings>>().Value;

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Knowledge API v1");
                options.RoutePrefix = "swagger";
            });
        }

        if (knowledgeSettings.HttpsRedirectEnabled)
        {
            app.UseHttpsRedirection();
        }

        return app;
    }

    /// <summary>
    /// Logs a welcome message with available endpoints after startup.
    /// </summary>
    public static WebApplication LogStartupComplete(this WebApplication app)
    {
        var knowledgeSettings = app.Services.GetRequiredService<IOptions<KnowledgeSettings>>().Value;
        var logger = app.Services.GetRequiredService<ILogger<KnowledgeSettings>>();
        var baseUrl = knowledgeSettings.PublicUrl.TrimEnd('/');

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
