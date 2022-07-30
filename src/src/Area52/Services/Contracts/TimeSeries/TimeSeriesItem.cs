namespace Area52.Services.Contracts.TimeSeries;

public class TimeSeriesItem
{
    public DateTime Time
    {
        get;
        init;
    }

    public double Value
    {
        get;
        init;
    }

    public TimeSeriesItem()
    {

    }

    public TimeSeriesItem(DateTime time, double value)
    {
        this.Time = time;
        this.Value = value;
    }
}
