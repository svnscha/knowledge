using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Knowledge.Shared.Logging;

/// <summary>
/// Options for configuring logging behavior.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Enable console logging (default: true for web apps).
    /// </summary>
    public bool EnableConsole { get; set; } = true;

    /// <summary>
    /// Enable file logging (default: false).
    /// </summary>
    public bool EnableFile { get; set; } = false;

    /// <summary>
    /// Path to the log file when file logging is enabled.
    /// </summary>
    public string LogFilePath { get; set; } = "logs/knowledge.log";

    /// <summary>
    /// Minimum log level for file output.
    /// </summary>
    public LogLevel FileLogLevel { get; set; } = LogLevel.Debug;
}

/// <summary>
/// Extension methods for configuring application logging.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configures logging with a clean, simplified console output format.
    /// </summary>
    public static ILoggingBuilder ConfigureSharedLogging(this ILoggingBuilder builder, LoggingOptions? options = null)
    {
        options ??= new LoggingOptions();

        builder.ClearProviders();

        if (options.EnableConsole)
        {
            builder.AddConsole(opt =>
            {
                opt.FormatterName = CleanConsoleFormatter.FormatterName;
            });
            builder.AddConsoleFormatter<CleanConsoleFormatter, CleanConsoleFormatterOptions>();
        }

        if (options.EnableFile)
        {
            // Ensure directory exists
            var logDir = Path.GetDirectoryName(options.LogFilePath);
            if (!string.IsNullOrEmpty(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            builder.AddProvider(new FileLoggerProvider(options.LogFilePath, options.FileLogLevel));
        }

        return builder;
    }
}

/// <summary>
/// Options for the clean console formatter.
/// </summary>
public class CleanConsoleFormatterOptions : ConsoleFormatterOptions
{
}

/// <summary>
/// A clean console formatter that produces human-readable log output.
/// </summary>
public class CleanConsoleFormatter : ConsoleFormatter
{
    public const string FormatterName = "clean";

    public CleanConsoleFormatter() : base(FormatterName)
    {
    }

    /// <inheritdoc />
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message is null)
        {
            return;
        }

        var timestamp = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss]");
        var logLevel = GetLogLevelString(logEntry.LogLevel);
        var category = SimplifyCategory(logEntry.Category);

        textWriter.WriteLine($"{timestamp} {logLevel}: {category} {message}");

        if (logEntry.Exception is not null)
        {
            textWriter.WriteLine(logEntry.Exception.ToString());
        }
    }

    private static string GetLogLevelString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "trce",
        LogLevel.Debug => "dbug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        LogLevel.None => "none",
        _ => "????"
    };

    private static string SimplifyCategory(string category)
    {
        // For Knowledge.* categories, use just the class name
        if (category.StartsWith("Knowledge.", StringComparison.Ordinal))
        {
            var lastDot = category.LastIndexOf('.');
            return lastDot >= 0 ? category[(lastDot + 1)..] : category;
        }

        // For Microsoft.Hosting.Lifetime, simplify to Hosting
        if (category.Equals("Microsoft.Hosting.Lifetime", StringComparison.Ordinal))
        {
            return "Hosting";
        }

        // For other Microsoft categories, use last segment
        if (category.StartsWith("Microsoft.", StringComparison.Ordinal))
        {
            var lastDot = category.LastIndexOf('.');
            return lastDot >= 0 ? category[(lastDot + 1)..] : category;
        }

        return category;
    }
}

/// <summary>
/// Simple file logger provider for writing logs to a file.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private StreamWriter? _writer;

    internal LogLevel MinLevel { get; }

    public FileLoggerProvider(string filePath, LogLevel minLevel)
    {
        _filePath = filePath;
        MinLevel = minLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, this);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    internal void WriteLog(string categoryName, LogLevel logLevel, string message, Exception? exception)
    {
        if (logLevel < MinLevel)
            return;

        lock (_lock)
        {
            try
            {
                _writer ??= new StreamWriter(_filePath, append: true) { AutoFlush = true };

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var level = logLevel switch
                {
                    LogLevel.Trace => "TRC",
                    LogLevel.Debug => "DBG",
                    LogLevel.Information => "INF",
                    LogLevel.Warning => "WRN",
                    LogLevel.Error => "ERR",
                    LogLevel.Critical => "CRT",
                    _ => "???"
                };

                _writer.WriteLine($"[{timestamp}] [{level}] [{categoryName}] {message}");

                if (exception is not null)
                {
                    _writer.WriteLine(exception.ToString());
                }
            }
            catch
            {
                _writer?.Dispose();
                _writer = null;
                throw;
            }
        }
    }
}

/// <summary>
/// Simple file logger implementation.
/// </summary>
public sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerProvider _provider;

    public FileLogger(string categoryName, FileLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _provider.MinLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _provider.WriteLog(_categoryName, logLevel, message, exception);
    }
}
