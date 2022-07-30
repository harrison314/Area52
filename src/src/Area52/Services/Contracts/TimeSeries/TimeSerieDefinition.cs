namespace Area52.Services.Contracts.TimeSeries;

public class TimeSerieDefinition
{
    public string Id
    {
        get;
        set;
    }

    public string Name
    {
        get;
        set;
    }

    public string Description
    {
        get;
        set;
    }

    public string Query
    {
        get;
        set;
    }

    public string? ValueFieldName
    {
        get;
        set;
    }

    public string? TagFieldName
    {
        get;
        set;
    }

    public AgregateFn DefaultAgregationFunction
    {
        get;
        set;
    }

    public ShowGraphTime ShowGraphTime
    {
        get;
        set;
    }

    public bool Enabled
    {
        get;
        set;
    }

    public LastExecutionInfo? LastExecutionInfo
    {
        get;
        set;
    }

    public UserObjectMetadata Metadata
    {
        get;
        set;
    }

    public TimeSerieDefinition()
    {
        this.Id = null!;
        this.Name = string.Empty;
        this.Description = string.Empty;
        this.Query = string.Empty;
        this.Metadata = new UserObjectMetadata();
    }
}
