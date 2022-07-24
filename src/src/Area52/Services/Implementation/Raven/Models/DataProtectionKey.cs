using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Implementation.Raven.Models;

public class DataProtectionKey
{
    public string FriendlyName
    {
        get;
        set;
    }

    public DateTime Created
    {
        get;
        set;
    }

    public DateTime? Expiration
    {
        get;
        set;
    }

    public string Xml
    {
        get;
        set;
    }

    public DataProtectionKey()
    {
        this.Xml = string.Empty;
        this.FriendlyName = string.Empty;
    }
}
