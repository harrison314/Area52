using System;

namespace Area52.Ufo.Services.Configuration;

public class SyslogSetup
{
    public bool Enabled
    {
        get;
        init;
    }

    public SyslogProtocol Protocol
    {
        get;
        init;
    }

    public string Host
    {
        get;
        init;
    }

    public int Port
    {
        get;
        init;
    }

    public AdditionalLogProperty[] AdditionalPropertys
    {
        get;
        init;
    }

    public SyslogSetup()
    {
        this.Host = string.Empty;
        this.AdditionalPropertys = Array.Empty<AdditionalLogProperty>();
    }
}
