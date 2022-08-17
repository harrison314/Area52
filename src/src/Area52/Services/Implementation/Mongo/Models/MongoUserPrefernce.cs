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

public class MongoUserPrefernce
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

    public MongoUserPrefernce()
    {
        this.SturedQuerys = null!;
        this.Metadata = null!;
        this.Version = 1;
    }
}
