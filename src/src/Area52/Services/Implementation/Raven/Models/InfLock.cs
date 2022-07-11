using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Implementation.Raven.Models;

public class InfLock
{
    public string Id
    {
        get;
        set;
    }

    public DateTime ExpireAt
    {
        get;
        set;
    }

    public InfLock()
    {
        this.Id = string.Empty;
    }

    internal InfLock(string id)
    {
        this.Id = id;
    }
}
