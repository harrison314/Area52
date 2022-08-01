using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Configuration;

public class TimeSeriesSetup
{
    public TimeSpan StartupDelay
    {
        get;
        set;
    }

    public TimeSpan CheckNewDataInterval
    {
        get;
        set;
    }

    public TimeSpan FindDataBeforeInterval
    {
        get;
        set;
    }

    public bool RemoveOldData
    {
        get;
        set;
    }

    public int RemovaDataAdDaysOld
    {
        get;
        set;
    }

    public TimeSeriesSetup()
    {

    }
}
