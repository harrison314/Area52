using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Configuration;

public class MongoDbSetup
{
    public MongoDatabaseSetup Database
    {
        get;
        set;
    }

    public MongoDbSetup()
    {
        this.Database = new MongoDatabaseSetup();
    }
}
