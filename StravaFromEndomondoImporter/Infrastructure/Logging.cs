namespace StravaFromEndomondoImporter.Infrastructure;

public static class Logging
{
    public static Logger Setup(string logPath)
    {
        var logFile = logPath;
        var logger = new LoggerConfiguration()
                     .WriteTo.File(Path.Combine(logFile, "endomondo-to-strava.txt"), restrictedToMinimumLevel: LogEventLevel.Verbose)
                     .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Verbose)
                     .CreateLogger();

        return logger;
    }
}