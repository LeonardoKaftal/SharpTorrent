using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace SharpTorrent.Utils;

public static class Singleton
{
    public static readonly HttpClient HttpClient = new();

    private static readonly Lazy<ILoggerFactory> LoggerFactory = new(() =>
        Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true; 
                options.TimestampFormat = "HH:mm:ss "; 
                options.UseUtcTimestamp = false; 
                options.ColorBehavior = LoggerColorBehavior.Enabled;
            })
        ));

    public static ILogger Logger => LoggerFactory.Value.CreateLogger("SharpTorrent");
    
    static Singleton()
    {
        HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }
}