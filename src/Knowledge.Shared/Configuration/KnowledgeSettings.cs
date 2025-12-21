using System.ComponentModel.DataAnnotations;

namespace Knowledge.Shared.Configuration;

/// <summary>
/// Strongly-typed configuration for application-level settings.
/// </summary>
/// <remarks>
/// </remarks>
public class KnowledgeSettings
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Knowledge";

    /// <summary>
    /// The application name displayed in logs and UI.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = "Knowledge";

    /// <summary>
    /// The public URL for the application (used for display/links).
    /// </summary>
    /// <remarks>
    /// </remarks>
    [Required]
    [Url]
    public string PublicUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Whether to enable HTTPS redirection middleware.
    /// </summary>
    public bool HttpsRedirectEnabled { get; set; } = false;

    /// <summary>
    /// Whether to enable detailed error messages in responses.
    /// </summary>
    /// <remarks>
    /// Should be false in production to avoid leaking implementation details. Only enable for debugging.
    /// </remarks>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// The minimum log level for the application.
    /// </summary>
    /// <remarks>
    /// Valid values: Trace, Debug, Information, Warning, Error, Critical, None.
    /// </remarks>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// The API key for external service authentication.
    /// </summary>
    /// <remarks>
    /// Use User Secrets (development) or Azure Key Vault / environment variables (production).
    /// This property exists for configuration binding; actual value should come from secure sources.
    /// </remarks>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The API endpoint URL for external services.
    /// </summary>
    /// <remarks>
    /// Leave empty to use the default endpoint for the configured service.
    /// Useful for pointing to alternative endpoints (Azure OpenAI, local models, etc.).
    /// </remarks>
    public string ApiEndpoint { get; set; } = string.Empty;
}
