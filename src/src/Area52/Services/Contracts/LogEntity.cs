namespace Area52.Services.Contracts;

public class LogEntity
{
    public DateTimeOffset Timestamp
    {
        get;
        set;
    }

    public string Message
    {
        get;
        set;
    }

    public string? MessageTemplate
    {
        get;
        set;
    }

    public string Level
    {
        get;
        set;
    }

    public int LevelNumeric
    {
        get;
        set;
    }

    public string? Exception
    {
        get;
        set;
    }

    public string? EventId
    {
        get;
        set;
    }

    public LogEntityProperty[] Properties
    {
        get;
        set;
    }

    public LogEntity()
    {
        this.Message = string.Empty;
        this.Level = string.Empty;
        this.MessageTemplate = string.Empty;
        this.Properties = Array.Empty<LogEntityProperty>();
    }
}
