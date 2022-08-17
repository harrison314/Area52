using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Configuration;

public class Area52Setup
{
    public int MaxLogShow
    {
        get;
        init;
    }

    public int? MaxErrorInClefBatch
    {
        get;
        init;
    }

    public Dictionary<string, bool> Features
    {
        get;
        init;
    }

    public Area52Setup()
    {
        this.Features = null!;
    }
}
