using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace Area52.Services.Implementation.Mongo.Models;

public class DbMigration
{
    [BsonId]
    public string Id
    {
        get;
        set;
    }

    public DateTime Executed
    {
        get;
        set;
    }

    public string AppVersion
    {
        get;
        set;
    }

    public DbMigration()
    {
        this.Id = string.Empty;
        this.AppVersion = string.Empty;
    }
}
