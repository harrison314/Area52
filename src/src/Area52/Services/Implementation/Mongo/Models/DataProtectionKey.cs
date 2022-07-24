using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Area52.Services.Implementation.Mongo.Models;

public class DataProtectionKey
{
    [BsonId]
    public ObjectId Id
    {
        get;
        set;
    }

    public string FriendlyName
    {
        get;
        set;
    }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Created
    {
        get;
        set;
    }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
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
