using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Knowledge.Shared.Logging;

public static class LoggingConfiguration
{
    public static ILoggingBuilder ConfigureSharedLogging(this ILoggingBuilder builder)
    {
        // Clear default providers and use our clean formatter
        builder.ClearProviders();
        builder.AddConsole(options =>
        {
            options.FormatterName = "clean";
        });
        builder.AddConsoleFormatter<CleanConsoleFormatter, CleanConsoleFormatterOptions>();

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
/// A clean console formatter that removes event IDs and simplifies category names.
/// </summary>
public class CleanConsoleFormatter : ConsoleFormatter
{
    public CleanConsoleFormatter() : base("clean")
    {
    }

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

        if (logEntry.Exception != null)
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
        _ => "????"
    };

    private static string SimplifyCategory(string category)
    {
        // For Knowledge.* categories, use just the class name
        if (category.StartsWith("Knowledge."))
        {
            var lastDot = category.LastIndexOf('.');
            return lastDot >= 0 ? category[(lastDot + 1)..] : category;
        }

        // For Microsoft.Hosting.Lifetime, simplify to Hosting
        if (category == "Microsoft.Hosting.Lifetime")
        {
            return "Hosting";
        }

        // For other Microsoft categories, use last segment
        if (category.StartsWith("Microsoft."))
        {
            var lastDot = category.LastIndexOf('.');
            return lastDot >= 0 ? category[(lastDot + 1)..] : category;
        }

        return category;
    }
}
