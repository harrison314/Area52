using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts.TimeSeries;
using Area52.Services.Implementation.Mongo.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo.TimeSeries;

public class TimeSeriesService : ITimeSeriesService
{
    private readonly IMongoDatabase mongoDatabase;
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger<TimeSeriesService> logger;

    public TimeSeriesService(IMongoDatabase mongoDatabase, ILoggerFactory loggerFactory)
    {
        this.mongoDatabase = mongoDatabase;
        this.loggerFactory = loggerFactory;
        this.logger = loggerFactory.CreateLogger<TimeSeriesService>();
    }

    public async Task<IReadOnlyList<TimeSeriesDefinitionId>> GetDefinitionForExecute(DateTime executeBefore, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetDefinitionForExecute with executeBefore={executeBefore}.", executeBefore);

        try
        {
            IMongoCollection<MongoTimeSerieDefinition> collection = this.mongoDatabase.GetCollection<MongoTimeSerieDefinition>(CollectionNames.MongoTimeSerieDefinition);
            FilterDefinition<MongoTimeSerieDefinition> filter = Builders<MongoTimeSerieDefinition>.Filter.Or(
                Builders<MongoTimeSerieDefinition>.Filter.Eq(t => t.LastExecutionInfo, null),
                Builders<MongoTimeSerieDefinition>.Filter.Lt(t => t.LastExecutionInfo!.LastExecute, executeBefore));

            using IAsyncCursor<ObjectId> cursor = await collection.FindAsync<ObjectId>(filter, new FindOptions<MongoTimeSerieDefinition, ObjectId>()
            {
                Projection = Builders<MongoTimeSerieDefinition>.Projection.Expression(t => t.Id)
            }, cancellationToken);

            List<ObjectId> ids = await cursor.ToListAsync(cancellationToken);

            var result = new List<TimeSeriesDefinitionId>(ids.Count);
            result.AddRange(ids.Select(t => new TimeSeriesDefinitionId(t.ToString())));

            return result;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in GetDefinitionForExecute with executeBefore {executeBefore}.", executeBefore);
            throw;
        }
    }

    public async Task<TimeSeriesDefinitionUnit> GetDefinitionUnit(string id, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetDefinitionUnit with id={id}.", id);

        try
        {
            IMongoCollection<MongoTimeSerieDefinition> collection = this.mongoDatabase.GetCollection<MongoTimeSerieDefinition>(CollectionNames.MongoTimeSerieDefinition);
            ObjectId objectId = new ObjectId(id);

            using IAsyncCursor<MongoTimeSerieDefinition> cursor = await collection.FindAsync(t => t.Id == objectId, null, cancellationToken);
            MongoTimeSerieDefinition definition =await cursor.SingleAsync(cancellationToken);

            return new TimeSeriesDefinitionUnit(definition.Id.ToString(),
                definition.Name,
                definition.Query,
                definition.ValueFieldName,
                definition.TagFieldName,
                definition.LastExecutionInfo?.LastExecute);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in GetDefinitionUnit method with id {id}.", id);
            throw;
        }
    }

    public Task<ITimeSeriesWriter> CreateWriter(string definitionId, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to CreateWriter with definitionId={definitionId}.", definitionId);

        try
        {
            IMongoCollection<MongoTimeSerieItem> collection = this.mongoDatabase.GetCollection<MongoTimeSerieItem>(CollectionNames.MongoTimeSerieItems);
            ObjectId id = new ObjectId(definitionId);
            ITimeSeriesWriter writer = new TimeSeriesWriter(collection, id, this.loggerFactory.CreateLogger<TimeSeriesWriter>());

            return Task.FromResult(writer);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in CreateWriter method with definitionId {definitionId}.", definitionId);
            throw;
        }
    }

    public async Task ConfirmWriting(string definitionId, LastExecutionInfo lastExecutionInfo, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to ConfirmWriting with definitionId={definitionId}.", definitionId);

        try
        {
            IMongoCollection<MongoTimeSerieDefinition> collection = this.mongoDatabase.GetCollection<MongoTimeSerieDefinition>(CollectionNames.MongoTimeSerieDefinition);
            ObjectId objectId = new ObjectId(definitionId);

            var filter = Builders<MongoTimeSerieDefinition>.Filter.Eq(t => t.Id, objectId);
            var update = Builders<MongoTimeSerieDefinition>.Update.Set(t => t.LastExecutionInfo, lastExecutionInfo);
            await collection.UpdateOneAsync(filter, update, null, cancellationToken);

            this.logger.LogInformation("Confirm writing time series data into definition with id {definitionId}.", definitionId);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in ConfirmWriting method with definitionId {definitionId}.", definitionId);
            throw;
        }
    }

    public async Task<IReadOnlyList<TimeSeriesItem>> ExecuteQuery(TimeSeriesQueryRequest request, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to ExecuteQuery with DefinitionId {DefinitionId}.", request.DefinitionId);
        try
        {
            BsonDocument agregationOperator = request.AgregationFunction switch
            {
                AggregateFn.Sum => new BsonDocument("$sum", "$Value"),
                AggregateFn.Min => new BsonDocument("$min", "$Value"),
                AggregateFn.Count => new BsonDocument("$count", new BsonDocument()),
                AggregateFn.Avg => new BsonDocument("$avg", "$Value"),
                AggregateFn.Max => new BsonDocument("$max", "$Value"),
                _ => throw new InvalidProgramException($"Enum value {request.AgregationFunction} is not supported.")
            };

            BsonDocument groupByExpression = request.GroupByFunction switch
            {
                TimeSeriesGroupByFn.Days => new BsonDocument("day", new BsonDocument("$dateTrunc", new BsonDocument()
                {
                            {"date", "$date" },
                            {"unit", "day" },
                            {"timezone", "UTC" }
                })),
                TimeSeriesGroupByFn.Hours => new BsonDocument("hour", new BsonDocument("$dateTrunc", new BsonDocument()
                {
                            {"date", "$date" },
                            {"unit", "hour" },
                            {"timezone", "UTC" }
                })),
                TimeSeriesGroupByFn.Minutes => new BsonDocument("minute", new BsonDocument("$dateTrunc", new BsonDocument()
                {
                            {"date", "$date" },
                            {"unit", "minute" },
                            {"timezone", "UTC" }
                })),
                TimeSeriesGroupByFn.Months => new BsonDocument("month", new BsonDocument("$dateTrunc", new BsonDocument()
                {
                            {"date", "$date" },
                            {"unit", "month" },
                            {"timezone", "UTC" }
                })),
                TimeSeriesGroupByFn.Quarters => new BsonDocument("quarter", new BsonDocument("$dateTrunc", new BsonDocument()
                {
                            {"date", "$date" },
                            {"unit", "quarter" },
                            {"timezone", "UTC" }
                })),
                TimeSeriesGroupByFn.Seconds => new BsonDocument("second", new BsonDocument("$dateTrunc", new BsonDocument()
                {
                            {"date","$date" },
                            {"unit", "second" },
                            {"timezone", "UTC" }
                })),
                TimeSeriesGroupByFn.Weeks => new BsonDocument("week", new BsonDocument("$dateTrunc", new BsonDocument()
                {
                            {"date","$date" },
                            {"unit", "week" },
                            {"startOfWeek", "monday" },
                            {"timezone", "UTC" }
                })),
                _ => throw new InvalidProgramException($"Enum value {request.GroupByFunction} is not supported.")
            };

            IMongoCollection<MongoTimeSerieItem> collection = this.mongoDatabase.GetCollection<MongoTimeSerieItem>(CollectionNames.MongoTimeSerieItems);
            ObjectId definitionId = new ObjectId(request.DefinitionId);
            BsonDocument[] stages = new BsonDocument[]
            {
                new BsonDocument("$match", new BsonDocument()
                {
                    { "$and", new BsonArray()
                       {
                         new BsonDocument("Timestamp", new BsonDocument("$gte", request.From )),
                         new BsonDocument("Timestamp", new BsonDocument("$lte", request.To )),
                         new BsonDocument("Meta.DefinitionId", new BsonDocument("$eq", definitionId))
                       }
                    }
                }),

                new BsonDocument("$project", new BsonDocument()
                {
                    {"date", "$Timestamp" },
                    {"Value", 1 },
                    {"Timestamp", 1 },
                }),

                new BsonDocument("$group", new BsonDocument()
                {
                    {"_id", groupByExpression},
                    {"Value", agregationOperator },
                    {"From", new BsonDocument("$min", "$Timestamp") },
                    {"To", new BsonDocument("$max", "$Timestamp") },
                }),

                new BsonDocument("$sort", new BsonDocument()
                {
                    {"From", 1 }
                })
            };

            this.logger.LogDebug("Executing aggregation stages {aggregation}.", new BsonArray(values: stages));
            using IAsyncCursor<BsonDocument> cursor = await collection.AggregateAsync<BsonDocument>(stages, null, cancellationToken);

            List<TimeSeriesItem> items = new List<TimeSeriesItem>();
            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (BsonDocument doc in cursor.Current)
                {
                    long from = doc["From"].ToUniversalTime().Ticks;
                    long to = doc["To"].ToUniversalTime().Ticks;
                    TimeSeriesItem item = new TimeSeriesItem()
                    {
                        Time = new DateTime(from + (to - from) / 2, DateTimeKind.Utc),
                        Value = this.GetNumricValue(doc["Value"])
                    };

                    items.Add(item);
                }
            }

            return items;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in ExecuteQuery method with DefinitionId {DefinitionId}.", request.DefinitionId);
            throw;
        }
        finally
        {
            this.logger.LogDebug("Exiting to ExecuteQuery with DefinitionId {DefinitionId}.", request.DefinitionId);
        }
    }

    private double GetNumricValue(BsonValue value)
    {
        if (value.IsInt32)
        {
            return (double)value.AsInt32;
        }

        if (value.IsInt64)
        {
            return (double)value.AsInt64;
        }

        if (value.IsDouble)
        {
            return (double)value.AsDouble;
        }

        throw new NotSupportedException($"Bson value {value} is not supported.");
    }
}
