using System.Threading;
using Area52.Services.Configuration;
using Area52.Services.Contracts;
using Area52.Services.Contracts.Statistics;
using Area52.Services.Implementation.Mongo.Models;
using Area52.Services.Implementation.Mongo.QueryTranslator;
using Area52.Services.Implementation.QueryParser;
using Area52.Services.Implementation.QueryParser.Nodes;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo;

public class LogDistributionServices : ILogDistributionServices
{
    private enum DistributionGranulation
    {
        Day,
        Hour
    }

    private readonly IMongoDatabase mongoDatabase;
    private readonly IOptions<Area52Setup> area52Setup;
    private readonly ILogger<LogDistributionServices> logger;

    public class TimestampModel
    {
        [BsonId]
        public ObjectId Id
        {
            get;
            set;
        }

        public DateTimeOffset Timestamp
        {
            get;
            set;
        }
    }

    public LogDistributionServices(IMongoDatabase mongoDatabase, IOptions<Area52Setup> area52Setup, ILogger<LogDistributionServices> logger)
    {
        this.mongoDatabase = mongoDatabase;
        this.area52Setup = area52Setup;
        this.logger = logger;
    }

    public async Task<LogsDistribution> GetLogsDistribution(string query)
    {
        this.logger.LogTrace("Entering to GetLogsDistribution with query {query}.", query);

        IAstNode? ast = string.IsNullOrWhiteSpace(query) ? null : Parser.SimpleParse(query);

        IMongoCollection<MongoLogEntity> collection = this.mongoDatabase.GetCollection<MongoLogEntity>(CollectionNames.LogEntities);
        //TODO: Debug log timer

        DateTime? startDate = await this.GetBoundary(collection, ast, true);
        DateTime? endDate = await this.GetBoundary(collection, ast, false);

        if (!startDate.HasValue || !endDate.HasValue)
        {
            return new LogsDistribution(new List<LogsDistributionItem>(), TimeSpan.FromDays(1.2));
        }

        TimeSpan timeDistance = endDate.Value - startDate.Value;
        LogViewSetup logViewSetup = this.area52Setup.Value.LogView;

        if (timeDistance < logViewSetup.DistributionMaxHourTimeInterval)
        {
            return await this.GetDaysDistribution(collection, ast, DistributionGranulation.Hour, null);
        }

        if (timeDistance <= logViewSetup.DistributionMaxTimeInterval)
        {
            return await this.GetDaysDistribution(collection, ast, DistributionGranulation.Day, null);
        }

        return await this.GetDaysDistribution(collection, ast, DistributionGranulation.Day, endDate.Value.Add(-logViewSetup.DistributionMaxTimeInterval));
    }

    //TODO: $group is extreme slow
    private async Task<LogsDistribution> GetDaysDistribution(IMongoCollection<MongoLogEntity> collection, IAstNode? astNode, DistributionGranulation distributionGranulation, DateTime? newStartDate)
    {
        this.logger.LogTrace("Enter to GetDaysDistribution.");

        IAstNode? updatedNode = (astNode != null, newStartDate.HasValue) switch
        {
            (true, true) => new AndNode(astNode!, new GtOrEqNode(new PropertyNode(nameof(LogEntity.Timestamp)), new StringValueNode(newStartDate!.Value.ToString(FormatConstants.SortableDateTimeFormat)))),
            (true, false) => astNode,
            (false, true) => new GtOrEqNode(new PropertyNode(nameof(LogEntity.Timestamp)), new StringValueNode(newStartDate!.Value.ToString(FormatConstants.SortableDateTimeFormat))),
            (false, false) => null,
        };

        string timeGroupField = distributionGranulation switch
        {
            DistributionGranulation.Day => "$TimestampIndex.DateTruncateUnixTime",
            DistributionGranulation.Hour => "$TimestampIndex.HourTruncateUnixTime",
            _ => throw new InvalidProgramException($"Enum value {distributionGranulation} is not supported.")
        };

        BsonDocument groupByDay = new BsonDocument()
        {
            {"_id", timeGroupField },
            { "Count", new  BsonDocument("$count", new BsonDocument()) }
        };

        BsonDocument[] stages;
        if (updatedNode == null)
        {
            stages = new BsonDocument[]
            {
                 new BsonDocument("$group", groupByDay),
                 new BsonDocument("$sort", new BsonDocument("_id", -1))
            };
        }
        else
        {
            BsonDocument findCriteria = this.BuildFindCriteria(updatedNode);

            stages = new BsonDocument[]
            {
                 new BsonDocument("$match", findCriteria),
                 new BsonDocument("$group", groupByDay),
                 new BsonDocument("$sort", new BsonDocument("_id", -1))
            };
        }

        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            this.logger.LogDebug("Translate input query in GetDaysDistribution to QRL {query}.", stages.ToJson());
        }

        List<LogsDistributionItem> result = new List<LogsDistributionItem>();
        using (IAsyncCursor<BsonDocument> cursor = await collection.AggregateAsync<BsonDocument>(stages, null, default))
        {
            foreach (BsonDocument document in await cursor.ToListAsync(default))
            {
                DateTime time = DateTimeOffset.FromUnixTimeSeconds( document["_id"].ToInt64()).UtcDateTime;
                decimal value = this.ConvertBsonTDecimal(document["Count"]);

                result.Add(new LogsDistributionItem(time, value));
            }
        }

        return new LogsDistribution(result, TimeSpan.FromDays(1.0));
    }

    private decimal ConvertBsonTDecimal(BsonValue value)
    {
        if (value.IsInt32)
        {
            return (decimal)value.AsInt32;
        }

        if (value.IsInt64)
        {
            return (decimal)value.AsInt64;
        }

        if (value.IsDouble)
        {
            return (decimal)value.AsDouble;
        }

        throw new InvalidProgramException("Bson value can not convert to double.");
    }

    private async Task<DateTime?> GetBoundary(IMongoCollection<MongoLogEntity> collection, IAstNode? ast, bool first)
    {
        this.logger.LogTrace("Enter to GetBoundary with first {first}.", first);

        BsonDocument findCriteria = this.BuildFindCriteria(ast);
        using var cursor = await collection.FindAsync<TimestampModel>(findCriteria, new FindOptions<MongoLogEntity, TimestampModel>()
        {
            Limit = 1,
            Sort = new BsonDocument("TimestampIndex.Utc", first ? 1 : -1),
            Projection = new BsonDocument()
            {
                { nameof(LogEntity.Timestamp), 1 }
            }
        });

        while (await cursor.MoveNextAsync())
        {
            foreach (TimestampModel entity in cursor.Current)
            {
                return entity.Timestamp.UtcDateTime;
            }
        }

        return null;
    }

    private BsonDocument BuildFindCriteria(IAstNode? astNode)
    {
        MongoDbNodeVisitor visitor = new MongoDbNodeVisitor();
        if (astNode != null)
        {
            visitor.Visit(astNode);
        }

        BsonDocument findCriteria = visitor.ToBsonFindCriteria();
        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            this.logger.LogDebug("Translate input query in BuildFindCriteria to QRL {query}.", findCriteria.ToJson());
        }

        return findCriteria;
    }
}
