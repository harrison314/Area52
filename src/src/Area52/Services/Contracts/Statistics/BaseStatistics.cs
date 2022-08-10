namespace Area52.Services.Contracts.Statistics;

public class BaseStatistics
{
    public long TotalLogCount
    {
        get;
        init;
    }

    public long NewLogsPerLastHour
    {
        get;
        init;
    }

    public int TimeSeriesCount
    {
        get;
        init;
    }

    public BackendType BackendType
    {
        get;
        init;
    }

    public TimeSpan ResponseTime
    {
        get;
        init;
    }

    public long ErrorsInLastHour
    {
        get;
        init;
    }

    public long CriticalInLastDay
    {
        get;
        init;
    }

    public BaseStatistics()
    {

    }
}
