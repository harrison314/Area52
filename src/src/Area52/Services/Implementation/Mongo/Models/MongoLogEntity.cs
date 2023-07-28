using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace Area52.Services.Implementation.Mongo.Models;

public class MongoLogEntity
{
    [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
    public ObjectId Id
    {
        get;
        set;
    }

    [BsonElement("v")]
    public int Version
    {
        get;
        set;
    }

    public LogEntityTimestamp TimestampIndex
    {
        get;
        set;
    }

    public string LogFullText
    {
        get;
        set;
    }

    public DateTimeOffset Timestamp
    {
        get;
        set;
    }

    public string? Message
    {
        get;
        set;
    }

    public string? MessageTemplate
    {
        get;
        set;
    }

    public string Level
    {
        get;
        set;
    }

    public string LevelLower
    {
        get;
        set;
    }

    public int LevelNumeric
    {
        get;
        set;
    }

    public string? Exception
    {
        get;
        set;
    }

    public string? EventId
    {
        get;
        set;
    }

    public LogEntityPropertyForMongo[] Properties
    {
        get;
        set;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public MongoLogEntity()
    {
        this.Version = 2;
        this.LogFullText = string.Empty;
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
