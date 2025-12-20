using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Logging;

public static class LoggingConfiguration
{
    public static ILoggingBuilder ConfigureSharedLogging(this ILoggingBuilder builder)
    {
        builder.AddSimpleConsole(options =>
        {
            options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
            options.SingleLine = false;
            options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
            options.IncludeScopes = true;
        });

        return builder;
    }
}
