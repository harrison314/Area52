namespace Area52.Services.Contracts;

public struct LogEntityProperty
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

    public double? Valued
    {
        get;
        set;
    }

    public LogEntityProperty(string name, string value)
    {
        this.Name = name;
        this.Values = value;
        this.Valued = null;
    }

    public LogEntityProperty(string name, double value)
    {
        this.Name = name;
        this.Values = null;
        this.Valued = value;
    }

    internal string GetValueString()
    {
        if (this.Values != null)
        {
            return this.Values;
        }

        if (this.Valued.HasValue)
        {
            return this.Valued.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return string.Empty;
    }
}