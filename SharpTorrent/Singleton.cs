using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace SharpTorrent;

public static class Singleton
{
    public static readonly HttpClient HttpClient = new();

    private static readonly Lazy<ILoggerFactory> LoggerFactory = new(() =>
        Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true; // Log su una riga
                options.TimestampFormat = "HH:mm:ss "; // Formato timestamp
                options.UseUtcTimestamp = false; // Usa il fuso orario locale
                options.ColorBehavior = LoggerColorBehavior.Enabled; // Abilita colori
            })
        ));

    public static ILogger Logger => LoggerFactory.Value.CreateLogger("SharpTorrent");
}
