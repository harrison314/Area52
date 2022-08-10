using Area52.Services.Contracts.Statistics;

namespace Area52.Services.Implementation.Raven.Statistics;

internal class LogLevelCalculator
{
    private readonly Dictionary<LogLevel, double> logs;

    public LogLevelCalculator()
    {
        this.logs = new Dictionary<LogLevel, double>()
        {
            { LogLevel.Critical, 0.0 },
            { LogLevel.Debug, 0.0 },
            { LogLevel.Error, 0.0 },
            { LogLevel.Information, 0.0 },
            { LogLevel.Trace, 0.0 },
            { LogLevel.Warning, 0.0 }
        };
    }

    public void SetResult(LogLevel level, int count)
    {
        this.logs[level] = (double)count;
    }

    public void SetNumericResult(string levelNumber, int count)
    {
        LogLevel logLevel = (LogLevel)int.Parse(levelNumber);
        this.logs[logLevel] = (double)count;
    }

    public List<LogShare> ToList()
    {
        decimal sum = (decimal)this.logs.Values.Sum();
        if (sum == 0.0m)
        {
            sum = 1.0m;
        }

        List<LogShare> result = new List<LogShare>();
        foreach ((LogLevel level, double count) in this.logs)
        {
            LogShare logPercent = new LogShare(level, 100.0m * ((decimal)count / sum));
            result.Add(logPercent);
        }

        return result;
    }
}
