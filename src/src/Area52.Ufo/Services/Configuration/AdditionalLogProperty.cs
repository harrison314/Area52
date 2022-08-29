namespace Area52.Ufo.Services.Configuration;

public class AdditionalLogProperty
{
    public string Name
    {
        get;
        init;
    }

    public object? Value
    {
        get;
        init;
    }

    public bool Override
    {
        get;
        init;
    }

    public AdditionalLogProperty()
    {
        this.Name = string.Empty;
    }
}
