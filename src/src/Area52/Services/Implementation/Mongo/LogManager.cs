using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Mongo.Models;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo;

public class LogManager : ILogManager
{
    private readonly IMongoDatabase mongoDatabase;
    private readonly ILogger<LogManager> logger;

    public LogManager(IMongoDatabase mongoDatabase, ILogger<LogManager> logger)
    {
        this.mongoDatabase = mongoDatabase;
        this.logger = logger;
    }

    public async Task RemoveOldLogs(DateTimeOffset timeAtDeteledLogs, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to RemoveOldLogs with timeAtDeteledLogs={timeAtDeteledLogs}", timeAtDeteledLogs);

        IMongoCollection<MongoLogEntity> collection = this.mongoDatabase.GetCollection<MongoLogEntity>("LogEntitys");
        DateTime urcTimeToDelete = timeAtDeteledLogs.UtcDateTime;

        _ = await collection.DeleteManyAsync(t => t.TimestampIndex.Utc < urcTimeToDelete, cancellationToken);

        this.logger.LogInformation("Removed old logs to {timeAtDeteledLogs}.", timeAtDeteledLogs);
    }
}
