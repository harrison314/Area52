using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Area52.Services.Implementation.Mongo.Models;

public class MongoTimeSerieItem
{
    [BsonElement("v")]
    public int Version
    {
        get;
        set;
    }

    public DateTime Timestamp
    {
        get;
        set;
    }

    public double Value
    {
        get;
        set;
    }

    public MongoTimeSerieItemMetaFiled Meta
    {
        get;
        set;
    }

    public MongoTimeSerieItem()
    {
        this.Version = 1;
        this.Meta = new MongoTimeSerieItemMetaFiled();
    }
}

public class MongoTimeSerieItemMetaFiled
{
    public string? Tag
    {
        get;
        set;
    }

    public ObjectId DefinitionId
    {
        get;
        set;
    }

    public MongoTimeSerieItemMetaFiled()
    {

    }
}
