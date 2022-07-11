using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Configuration;

public class ArchiveSettings
{
    public bool Enabled
    {
        get;
        set;
    }

    public TimeSpan CheckInterval
    {
        get;
        set;
    }

    public int RemovaLogsAdDaysOld
    {
        get;
        set;
    }

    public ArchiveSettings()
    {

    }
}
