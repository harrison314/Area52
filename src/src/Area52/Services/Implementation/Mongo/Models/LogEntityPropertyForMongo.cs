namespace Area52.Services.Implementation.Mongo.Models;

public class LogEntityPropertyForMongo
{
    public string Name
    {
        get;
        set;
    }

    public string? Values
    {
        get;
        set;
    }

    public string? ValuesLower
    {
        get;
        set;
    }

    public double? Valued
    {
        get;
        set;
    }

    public LogEntityPropertyForMongo()
    {
        this.Name = string.Empty;
    }
}
