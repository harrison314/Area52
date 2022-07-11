using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Configuration;

public class RavenDbSetup
{
    public RavenDatabaseSettings Database 
    { 
        get; 
        set; 
    }

    public RavenDbSetup()
    {
        this.Database = new RavenDatabaseSettings();
    }
}
