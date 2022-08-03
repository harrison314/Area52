namespace Area52.Services.Contracts.TimeSeries;

public class TimeSeriesQueryRequest
{
    public string DefinitionId
    {
        get;
        init;
    }

    public DateTime From
    {
        get;
        init;
    }

    public DateTime To
    {
        get;
        init;
    }

    public TimeSeriesGroupByFn GroupByFunction
    {
        get;
        init;
    }

    public AggregateFn AgregationFunction
    {
        get;
        init;
    }

    public TimeSeriesQueryRequest()
    {
        this.DefinitionId = string.Empty;
    }
}
