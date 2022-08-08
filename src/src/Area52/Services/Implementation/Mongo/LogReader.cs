using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Mongo.Models;
using Area52.Services.Implementation.Mongo.QueryTranslator;
using Area52.Services.Implementation.QueryParser;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo;

public class LogReader : ILogReader
{
    private readonly IMongoDatabase mongoDatabase;
    private readonly ILogger<LogReader> logger;

    public LogReader(IMongoDatabase mongoDatabase, ILogger<LogReader> logger)
    {
        this.mongoDatabase = mongoDatabase;
        this.logger = logger;
    }

    public async Task<LogEntity?> LoadLogInfo(string id)
    {
        this.logger.LogTrace("Entering to LoadLogInfo with id {logENtityId}", id);

        IMongoCollection<MongoLogEntity> collection = this.mongoDatabase.GetCollection<MongoLogEntity>(CollectionNames.LogEntities);

        MongoDB.Bson.ObjectId objectId = new MongoDB.Bson.ObjectId(id);
        using IAsyncCursor<MongoLogEntity> result = await collection.FindAsync(t => t.Id == objectId);

        return this.MapFrom(await result.SingleOrDefaultAsync());
    }

    public async Task<ReadLastLogResult> ReadLastLogs(string query)
    {
        this.logger.LogTrace("Entering to ReadLastLogs with query {query}.", query);

        try
        {
            MongoDbNodeVisitor visitor = new MongoDbNodeVisitor();

            if (!string.IsNullOrEmpty(query))
            {
                IAstNode astNode = Parser.SimpleParse(query);
                visitor.Visit(astNode);
            }

            BsonDocument findCriteria = visitor.ToBsonFindCriteria();

            if (this.logger.IsEnabled(LogLevel.Debug))
            {
                this.logger.LogDebug("Translate query {query} to find criteria {findCriteria}.", query, findCriteria.ToJson());
            }

            IMongoCollection<MongoLogEntity> collection = this.mongoDatabase.GetCollection<MongoLogEntity>(CollectionNames.LogEntities);
            using var cursor = await collection.FindAsync<LogInfo>(findCriteria, new FindOptions<MongoLogEntity, LogInfo>()
            {
                Limit = 150,
                Sort = new BsonDocument("TimestampIndex.Utc", -1),
                Projection = new BsonDocument()
            {
                {"Id", new BsonDocument("$toString", "Id") },
                {"Timestamp", 1 },
                {"Level", 1 },
                {"Message", 1 },
            }
            });

            long totalCount = await collection.CountDocumentsAsync(findCriteria);

            return new ReadLastLogResult(await cursor.ToListAsync(), totalCount);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Error during ReadLastLogs with query {query}.", query);
            throw;
        }
    }

    public IAsyncEnumerable<LogEntity> ReadLogs(string query, int? limit)
    {
        this.logger.LogTrace("Entering to ReadLogs with query {query}, limit {limit}.", query, limit);

        IAstNode? node = string.IsNullOrWhiteSpace(query) ? null : Parser.SimpleParse(query);
        return this.ReadLogsInternal(node, limit);
    }

    public IAsyncEnumerable<LogEntity> ReadLogs(IAstNode query, int? limit)
    {
        this.logger.LogTrace("Entering to ReadLogs with ast nodes, limit {limit}.", limit);
        return this.ReadLogsInternal(query, limit);
    }

    private async IAsyncEnumerable<LogEntity> ReadLogsInternal(IAstNode? query, int? limit)
    {
        MongoDbNodeVisitor visitor = new MongoDbNodeVisitor();
        if (query != null)
        {
            visitor.Visit(query);
        }

        BsonDocument findCriteria = visitor.ToBsonFindCriteria();

        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            this.logger.LogDebug("Translate query {query} to find criteria {findCriteria}.", query, findCriteria.ToJson());
        }

        IMongoCollection<MongoLogEntity> collection = this.mongoDatabase.GetCollection<MongoLogEntity>(CollectionNames.LogEntities);
        using var cursor = await collection.FindAsync<MongoLogEntity>(findCriteria, new FindOptions<MongoLogEntity, MongoLogEntity>()
        {
            Limit = limit,
            Sort = new BsonDocument("TimestampIndex.Utc", -1),
            Projection = new BsonDocument()
            {
                { nameof(LogEntity.EventId), 1 },
                { nameof(LogEntity.Exception), 1 },
                { nameof(LogEntity.Timestamp), 1 },
                { nameof(LogEntity.Level), 1 },
                { nameof(LogEntity.LevelNumeric), 1 },
                { nameof(LogEntity.Message), 1 },
                { nameof(LogEntity.MessageTemplate), 1 },
                { nameof(LogEntity.Properties), 1 },
            }
        });

        while (await cursor.MoveNextAsync())
        {
            foreach (MongoLogEntity entity in cursor.Current)
            {
                yield return this.MapFrom(entity);
            }
        }
    }

    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("entity")]
    private LogEntity? MapFrom(MongoLogEntity? entity)
    {
        if (entity == null)
        {
            return null;
        }

        return new LogEntity()
        {
            EventId = entity.EventId,
            Exception = entity.Exception,
            Level = entity.Level,
            LevelNumeric = entity.LevelNumeric,
            Message = entity.Message ?? string.Empty,
            MessageTemplate = entity.MessageTemplate,
            Properties = this.MapFrom(entity.Properties),
            Timestamp = entity.Timestamp
        };
    }

    private LogEntityProperty[] MapFrom(LogEntityPropertyForMongo[] array)
    {
        if (array.Length == 0)
        {
            return Array.Empty<LogEntityProperty>();
        }

        LogEntityProperty[] result = new LogEntityProperty[array.Length];
        for (int i = 0; i < result.Length; i++)
        {
            LogEntityPropertyForMongo entity = array[i];
            if (entity.Valued.HasValue)
            {
                result[i] = new LogEntityProperty(entity.Name, entity.Valued.Value);
            }
            else
            {
                result[i] = new LogEntityProperty(entity.Name, entity.Values!);
            }
        }

        return result;
    }
}
