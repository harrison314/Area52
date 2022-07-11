namespace Area52.Services.Implementation.Raven;

internal static partial class LogWriterLogExtensions
{
    [LoggerMessage(Level = LogLevel.Trace, Message = "Entering to Write.")]
    public static partial void EnteringToWrite(this ILogger<LogWriter> logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sucessfull writed {logCount} logs.")]
    public static partial void SucessufullWritedLogs(this ILogger<LogWriter> logger, int logCount);
}