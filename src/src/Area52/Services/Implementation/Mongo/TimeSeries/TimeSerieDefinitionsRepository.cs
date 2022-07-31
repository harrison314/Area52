﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts.TimeSeries;
using Area52.Services.Implementation.Mongo.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo.TimeSeries;

public class TimeSerieDefinitionsRepository : ITimeSerieDefinitionsRepository
{
    private readonly IMongoDatabase mongoDatabase;
    private readonly ILogger<TimeSerieDefinitionsRepository> logger;

    public TimeSerieDefinitionsRepository(IMongoDatabase mongoDatabase, ILogger<TimeSerieDefinitionsRepository> logger)
    {
        this.mongoDatabase = mongoDatabase;
        this.logger = logger;
    }

    public async Task<string> Create(TimeSerieDefinition timeSerieDefinition)
    {
        this.logger.LogTrace("Entering to Create.");

        try
        {
            MongoTimeSerieDefinition model = this.Map(timeSerieDefinition);

            IMongoCollection<MongoTimeSerieDefinition> collection = this.mongoDatabase.GetCollection<MongoTimeSerieDefinition>(CollectionNames.MongoTimeSerieDefinition);
            await collection.InsertOneAsync(model);

            return model.Id.ToString();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexcepted error in Create method.");
            throw;
        }
    }

    public async Task<TimeSerieDefinition> FindById(string id)
    {
        this.logger.LogTrace("Entering to FindById with id {id}.", id);

        try
        {
            IMongoCollection<MongoTimeSerieDefinition> collection = this.mongoDatabase.GetCollection<MongoTimeSerieDefinition>(CollectionNames.MongoTimeSerieDefinition);
            ObjectId objectId = new ObjectId(id);
            MongoTimeSerieDefinition definition = collection.AsQueryable().Where(t => t.Id == objectId).Single();

            return this.Map(definition);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexcepted error in FindById method with id {id}.", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<TimeSerieDefinitionInfo>> FindDefinictions()
    {
        this.logger.LogTrace("Entering to FindDefinictions.");

        try
        {
            IMongoCollection<MongoTimeSerieDefinition> collection = this.mongoDatabase.GetCollection<MongoTimeSerieDefinition>(CollectionNames.MongoTimeSerieDefinition);
            return collection.AsQueryable().Select(t => new TimeSerieDefinitionInfo()
            {
                Id = t.Id.ToString(),
                Name = t.Name,
                Description = t.Description
            }).ToList();

        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexcepted error in FindDefinictions method.");
            throw;
        }
    }

    private MongoTimeSerieDefinition Map(TimeSerieDefinition timeSerieDefinition)
    {
        return new MongoTimeSerieDefinition()
        {
            DefaultAgregationFunction = timeSerieDefinition.DefaultAgregationFunction,
            Description = timeSerieDefinition.Description,
            Enabled = timeSerieDefinition.Enabled,
            LastExecutionInfo = timeSerieDefinition.LastExecutionInfo,
            Metadata = timeSerieDefinition.Metadata,
            Name = timeSerieDefinition.Name,
            Query = timeSerieDefinition.Query,
            ShowGraphTime = timeSerieDefinition.ShowGraphTime,
            TagFieldName = timeSerieDefinition.TagFieldName,
            ValueFieldName = timeSerieDefinition.ValueFieldName
        };
    }

    private TimeSerieDefinition Map(MongoTimeSerieDefinition timeSerieDefinition)
    {
        return new TimeSerieDefinition()
        {
            DefaultAgregationFunction = timeSerieDefinition.DefaultAgregationFunction,
            Description = timeSerieDefinition.Description,
            Enabled = timeSerieDefinition.Enabled,
            LastExecutionInfo = timeSerieDefinition.LastExecutionInfo,
            Metadata = timeSerieDefinition.Metadata,
            Name = timeSerieDefinition.Name,
            Query = timeSerieDefinition.Query,
            ShowGraphTime = timeSerieDefinition.ShowGraphTime,
            TagFieldName = timeSerieDefinition.TagFieldName,
            ValueFieldName = timeSerieDefinition.ValueFieldName,
            Id = timeSerieDefinition.Id.ToString()
        };
    }
}
