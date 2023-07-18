namespace Area52.Services.Configuration;

public class LogViewSetup
{
    public int MaxLogShow
    {
        get;
        init;
    }

    public TimeSpan DistributionMaxHourTimeInterval
    {
        get;
        init;
    }

    public TimeSpan DistributionMaxTimeInterval
    {
        get;
        init;
    }

    public LogViewSetup()
    {
        
    }
}
