using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;

namespace Area52.Services.Implementation.Raven.Models;

public class UserPrefernce
{
    public List<SturedQuery> SturedQuerys
    {
        get;
        set;
    }

    public UserObjectMetadata Metadata
    {
        get;
        set;
    }

    public UserPrefernce()
    {
        this.SturedQuerys = null!;
        this.Metadata = null!;
    }
}
