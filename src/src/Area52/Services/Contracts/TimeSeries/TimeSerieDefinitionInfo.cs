namespace Area52.Services.Contracts.TimeSeries;

public class TimeSerieDefinitionInfo
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

    public TimeSerieDefinitionInfo()
    {
        this.Id = string.Empty;
        this.Name = string.Empty;
        this.Description = string.Empty;
    }

    public TimeSerieDefinitionInfo(string id, string name, string description)
    {
        this.Id = id;
        this.Name = name;
        this.Description = description;
    }
}
