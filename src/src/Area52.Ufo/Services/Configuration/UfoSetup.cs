using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Ufo.Services.Configuration;

public class UfoSetup
{
    public string Area52Endpoint
    {
        get;
        init;
    }

    public string? ApiKey
    {
        get;
        init;
    }

    public int RecomandetBatchSize
    {
        get;
        init;
    }

    public UfoSetup()
    {
        this.Area52Endpoint = string.Empty;
    }
}
