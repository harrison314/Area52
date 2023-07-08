using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Mongo.Models;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo;

public class LogWriter : ILogWriter
{
    private readonly IMongoDatabase mongoDatabase;
    private readonly ILogger<LogWriter> logger;

    public LogWriter(IMongoDatabase mongoDatabase, ILogger<LogWriter> logger)
    {
        this.mongoDatabase = mongoDatabase;
        this.logger = logger;
    }

    public async Task Write(IReadOnlyList<LogEntity> logs)
    {
        this.logger.LogTrace("Entering to Write.");
        IMongoCollection<MongoLogEntity> collection = this.mongoDatabase.GetCollection<MongoLogEntity>(CollectionNames.LogEntities);
        await collection.InsertManyAsync(this.ToModels(logs));

        this.logger.LogInformation("Successful write {logsCount} logs into MongoDb.", logs.Count);
    }

    private IEnumerable<MongoLogEntity> ToModels(IReadOnlyList<LogEntity> logs)
    {
        for (int i = 0; i < logs.Count; i++)
        {
            LogEntity entity = logs[i];

            MongoLogEntity item = new MongoLogEntity()
            {
                EventId = entity.EventId,
                Exception = entity.Exception,
                LogFullText = string.Concat(entity.Timestamp.ToString(FormatConstants.SortableDateTimeFormat),
                                   " ",
                                   entity.Level,
                                   " ",
                                   entity.Message,
                                   entity.Exception),
                Level = entity.Level,
                LevelLower = entity.Level.ToLowerInvariant(),
                LevelNumeric = entity.LevelNumeric,
                Message = entity.Message,
                MessageTemplate = entity.MessageTemplate,
                Properties = new LogEntityPropertyForMongo[entity.Properties.Length],
                Timestamp = entity.Timestamp,
                TimestampIndex = new LogEntityTimestamp(entity.Timestamp),
            };

            for (int j = 0; j < item.Properties.Length; j++)
            {
                LogEntityProperty p = entity.Properties[j];
                LogEntityPropertyForMongo pr = new LogEntityPropertyForMongo();
                pr.Name = p.Name;

                if (p.Valued.HasValue)
                {
                    pr.Valued = p.Valued.Value;
                    pr.Values = p.Valued.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(p.Values is not null);
                    pr.Values = p.Values;
                    pr.ValuesLower = p.Values.ToLowerInvariant();
                }

                item.Properties[j] = pr;
            }

            yield return item;
        }
    }
}
