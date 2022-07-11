namespace Area52.Services.Configuration;

public class RavenDatabaseSettings
{
    public string[] Urls 
    { 
        get; 
        set; 
    }

    public string DatabaseName 
    {
        get; 
        set;
    }

    public string? CertPath 
    { 
        get; 
        set; 
    }

    public string? CertPass 
    { 
        get; 
        set;
    }

    public RavenDatabaseSettings()
    {
        this.Urls = Array.Empty<string>();
        this.DatabaseName = string.Empty;
    }
}