using Microsoft.Extensions.Configuration;

namespace Knowledge.Shared.Configuration;

/// <summary>
/// Configuration provider that reads from .env files.
/// Integrates with the standard Microsoft.Extensions.Configuration pipeline.
/// Loads {Environment}.env if it exists, otherwise falls back to .env.
/// </summary>
public class DotEnvConfigurationProvider : ConfigurationProvider
{
    private readonly string? _filePath;
    private readonly string? _environment;
    private readonly bool _optional;
    private readonly string? _basePath;

    /// <summary>
    /// Gets the path of the loaded .env file, or null if none was loaded.
    /// </summary>
    public string? LoadedFile { get; private set; }

    public DotEnvConfigurationProvider(string? filePath, string? environment, bool optional, string? basePath = null)
    {
        _filePath = filePath;
        _environment = environment;
        _optional = optional;
        _basePath = basePath;
    }

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        string? envFilePath;

        if (_filePath != null)
        {
            // Explicit path provided - load only that file
            envFilePath = _filePath;
        }
        else
        {
            // Try environment-specific file first (e.g., Development.env), then fall back to .env
            envFilePath = !string.IsNullOrEmpty(_environment)
                ? FindEnvFile($"{_environment}.env") ?? FindEnvFile(".env")
                : FindEnvFile(".env");
        }

        if (envFilePath == null || !File.Exists(envFilePath))
        {
            if (!_optional)
            {
                throw new FileNotFoundException("No .env file was found and configuration is not optional.");
            }
            LoadedFile = null;
            data["DotEnv:LoadedFile"] = string.Empty;
            Data = data;
            return;
        }

        LoadedFile = Path.GetFullPath(envFilePath);

        foreach (var line in File.ReadAllLines(envFilePath))
        {
            var parsed = ParseLine(line);
            if (parsed.HasValue)
            {
                var (key, value) = parsed.Value;
                // Convert POSTGRES_USER to Postgres:User format for standard .NET config binding
                var configKey = ConvertToConfigurationKey(key);
                data[configKey] = value;

                // Also keep the original key for direct access
                data[key] = value;
            }
        }

        // Store loaded file path in configuration for logging purposes
        data["DotEnv:LoadedFile"] = LoadedFile;

        Data = data;
    }

    /// <summary>
    /// Converts environment variable style keys (POSTGRES_USER) to configuration style (Postgres:User).
    /// </summary>
    private static string ConvertToConfigurationKey(string key)
    {
        // Handle common prefixes
        var parts = key.Split('_');
        if (parts.Length < 2)
        {
            return key;
        }

        // Convert POSTGRES_USER -> Postgres:User
        // Convert PGADMIN_DEFAULT_EMAIL -> PgAdmin:DefaultEmail
        var result = new List<string>();
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            result.Add(char.ToUpper(part[0]) + part[1..].ToLower());
        }

        // Group by known prefixes
        if (result.Count >= 2)
        {
            var prefix = result[0];
            var rest = string.Join("", result.Skip(1));
            return $"{prefix}:{rest}";
        }

        return string.Join(":", result);
    }

    /// <summary>
    /// Searches for a specific .env file in current directory and parent directories.
    /// Also searches from the application base directory to handle --project scenarios.
    /// </summary>
    private string? FindEnvFile(string fileName)
    {
        // Start from base path if provided, otherwise try multiple starting points
        var searchPaths = new List<string?>();

        // Priority 1: Explicit base path (e.g., content root)
        if (!string.IsNullOrEmpty(_basePath))
        {
            searchPaths.Add(_basePath);
        }

        // Priority 2: Application base directory (handles dotnet run --project)
        searchPaths.Add(AppContext.BaseDirectory);

        // Priority 3: Current working directory
        searchPaths.Add(Directory.GetCurrentDirectory());

        foreach (var startPath in searchPaths.Where(p => !string.IsNullOrEmpty(p)))
        {
            var checkDevContainer = string.Equals(_environment, "Development", StringComparison.OrdinalIgnoreCase);
            var result = SearchFromDirectory(startPath!, fileName, checkDevContainer);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Searches for a specific .env file starting from a directory and walking up to parents.
    /// </summary>
    private static string? SearchFromDirectory(string startDirectory, string fileName, bool checkDevContainer)
    {
        var directory = startDirectory;

        while (directory != null)
        {
            var envPath = Path.Combine(directory, fileName);
            if (File.Exists(envPath))
            {
                return envPath;
            }

            // Only check .devcontainer folder in Development environment
            if (checkDevContainer)
            {
                var devContainerEnvPath = Path.Combine(directory, ".devcontainer", fileName);
                if (File.Exists(devContainerEnvPath))
                {
                    return devContainerEnvPath;
                }
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        return null;
    }

    /// <summary>
    /// Parses a single line from the .env file.
    /// </summary>
    private static (string Key, string Value)? ParseLine(string line)
    {
        var trimmedLine = line.Trim();

        // Skip empty lines and comments
        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
        {
            return null;
        }

        var separatorIndex = trimmedLine.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return null;
        }

        var key = trimmedLine[..separatorIndex].Trim();
        var value = trimmedLine[(separatorIndex + 1)..].Trim();

        // Remove surrounding quotes if present
        if ((value.StartsWith('"') && value.EndsWith('"')) ||
            (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value[1..^1];
        }

        return (key, value);
    }
}

/// <summary>
/// Configuration source for .env files.
/// </summary>
public class DotEnvConfigurationSource : IConfigurationSource
{
    public string? Path { get; set; }
    public string? Environment { get; set; }
    public bool Optional { get; set; } = true;
    public string? BasePath { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        // Use FileProvider's root if available and no explicit base path
        var basePath = BasePath;
        if (string.IsNullOrEmpty(basePath) && builder.Properties.TryGetValue("FileProvider", out var fileProviderObj))
        {
            if (fileProviderObj is Microsoft.Extensions.FileProviders.PhysicalFileProvider physicalProvider)
            {
                basePath = physicalProvider.Root;
            }
        }

        return new DotEnvConfigurationProvider(Path, Environment, Optional, basePath);
    }
}

/// <summary>
/// Extension methods for adding .env file support to IConfigurationBuilder.
/// </summary>
public static class DotEnvConfigurationExtensions
{
    /// <summary>
    /// Adds .env file(s) as configuration sources.
    /// Loads .env first, then .env.{environment} to override values.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="environment">The environment name (Development, Production, etc.). If provided, also loads .env.{environment}.</param>
    /// <param name="optional">Whether the files are optional. Defaults to true.</param>
    /// <param name="basePath">Base path to start searching from. If null, uses content root.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddDotEnvFile(this IConfigurationBuilder builder, string? environment = null, bool optional = true, string? basePath = null)
    {
        return builder.Add(new DotEnvConfigurationSource
        {
            Environment = environment,
            Optional = optional,
            BasePath = basePath
        });
    }

    /// <summary>
    /// Adds a specific .env file as a configuration source.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="path">Path to the .env file.</param>
    /// <param name="optional">Whether the file is optional. Defaults to true.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddDotEnvFile(this IConfigurationBuilder builder, string path, bool optional = true)
    {
        return builder.Add(new DotEnvConfigurationSource
        {
            Path = path,
            Optional = optional
        });
    }
}
