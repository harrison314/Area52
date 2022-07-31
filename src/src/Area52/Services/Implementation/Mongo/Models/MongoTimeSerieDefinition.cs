using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Area52.Services.Contracts.TimeSeries;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Area52.Services.Implementation.Mongo.Models;

public class MongoTimeSerieDefinition
{
    [BsonId]
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

    public string Name
    {
        get;
        set;
    }

    public string Description
    {
        get;
        set;
    }

    public string Query
    {
        get;
        set;
    }

    public string? ValueFieldName
    {
        get;
        set;
    }

    public string? TagFieldName
    {
        get;
        set;
    }

    public AgregateFn DefaultAgregationFunction
    {
        get;
        set;
    }

    public ShowGraphTime ShowGraphTime
    {
        get;
        set;
    }

    public bool Enabled
    {
        get;
        set;
    }

    public LastExecutionInfo? LastExecutionInfo
    {
        get;
        set;
    }

    public UserObjectMetadata Metadata
    {
        get;
        set;
    }

    public MongoTimeSerieDefinition()
    {
        this.Version = 1;
    }
}
