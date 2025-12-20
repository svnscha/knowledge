namespace Knowledge.Shared.Configuration;

/// <summary>
/// Strongly-typed configuration for application-level settings.
/// </summary>
public class KnowledgeSettings
{
    public const string SectionName = "Knowledge";

    /// <summary>
    /// The application name.
    /// </summary>
    public string Name { get; set; } = "Knowledge";

    /// <summary>
    /// The base URL for the application (used for display/links).
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Whether to enable HTTPS redirection.
    /// </summary>
    public bool HttpsRedirectEnabled { get; set; } = false;

    /// <summary>
    /// Whether to enable detailed error messages.
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// The log level for the application.
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}

/// <summary>
/// Extension methods to build KnowledgeSettings from environment variables.
/// </summary>
public static class KnowledgeSettingsExtensions
{
    /// <summary>
    /// Creates KnowledgeSettings from environment variables (APP_* format).
    /// </summary>
    public static KnowledgeSettings FromEnvironment()
    {
        return new KnowledgeSettings
        {
            Name = Environment.GetEnvironmentVariable("APP_NAME") ?? "Knowledge",
            BaseUrl = Environment.GetEnvironmentVariable("APP_BASE_URL") ?? "http://localhost:5000",
            HttpsRedirectEnabled = bool.TryParse(Environment.GetEnvironmentVariable("APP_HTTPS_REDIRECT_ENABLED"), out var httpsEnabled) && httpsEnabled,
            EnableDetailedErrors = bool.TryParse(Environment.GetEnvironmentVariable("APP_ENABLE_DETAILED_ERRORS"), out var enableErrors) && enableErrors,
            LogLevel = Environment.GetEnvironmentVariable("APP_LOG_LEVEL") ?? "Information"
        };
    }
}
