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


    /// <summary>
    /// The API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The API endpoint.
    /// </summary>
    public string ApiEndpoint { get; set; } = string.Empty;
}
