using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts.Statistics;
using MongoDB.Driver;
using Area52.Services.Implementation.Mongo.Models;
using MongoDB.Bson;

namespace Area52.Services.Implementation.Mongo.Statistics;

public class FastStatisticsServices : IFastStatisticsServices
{
    private readonly IMongoDatabase mongoDatabase;
    private readonly ILogger<FastStatisticsServices> logger;

    public FastStatisticsServices(IMongoDatabase mongoDatabase, ILogger<FastStatisticsServices> logger)
    {
        this.mongoDatabase = mongoDatabase;
        this.logger = logger;
    }

    public async Task<BaseStatistics> GetBaseStatistics(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetBaseStatistics.");

        try
        {
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            DateTime lastHour = utcNow.AddHours(-1.0).DateTime;
            DateTime lastDay = utcNow.AddHours(-24.0).DateTime;
            int errorLogLevel = (int)LogLevel.Error;
            int criticalLogLevel = (int)LogLevel.Critical;

            IMongoCollection<MongoLogEntity> logCollection = this.mongoDatabase.GetCollection<MongoLogEntity>(CollectionNames.LogEntities);
            IMongoCollection<MongoTimeSerieDefinition> tsCollection = this.mongoDatabase.GetCollection<MongoTimeSerieDefinition>(CollectionNames.MongoTimeSeriesDefinition);

            DateTime executionStart = DateTime.UtcNow;
            long totalLogCount = await logCollection.CountDocumentsAsync(Builders<MongoLogEntity>.Filter.Empty, null, cancellationToken);
            long newLogsPerLastHour = await logCollection.CountDocumentsAsync(t => t.TimestampIndex.Utc >= lastHour, null, cancellationToken);
            long timeSeriesCount = await tsCollection.CountDocumentsAsync(Builders<MongoTimeSerieDefinition>.Filter.Empty, null, cancellationToken);
            long errorsInLastHour = await logCollection.CountDocumentsAsync(t => t.TimestampIndex.Utc >= lastHour && t.LevelNumeric == errorLogLevel, null, cancellationToken);
            long criticalInLastDay = await logCollection.CountDocumentsAsync(t => t.TimestampIndex.Utc >= lastDay && t.LevelNumeric == criticalLogLevel, null, cancellationToken);

            TimeSpan executionTime = DateTime.UtcNow - executionStart;

            this.logger.LogDebug("Get statistics from {logsCount} logs in {duration}.",
                totalLogCount,
                executionTime);

            return new BaseStatistics()
            {
                TotalLogCount = totalLogCount,
                NewLogsPerLastHour = newLogsPerLastHour,
                TimeSeriesCount = Convert.ToInt32(timeSeriesCount),
                BackendType = BackendType.MongoDb,
                ResponseTime = executionTime,
                ErrorsInLastHour = errorsInLastHour,
                CriticalInLastDay = criticalInLastDay
            };
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error during executionTime GetBaseStatistics.");
            throw;
        }
    }

    public async Task<IReadOnlyList<LogShare>> GetLevelsDistribution(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetLevelsDistribution.");

        try
        {
            IMongoCollection<MongoLogEntity> logCollection = this.mongoDatabase.GetCollection<MongoLogEntity>(CollectionNames.LogEntities);

            BsonDocument[] stages = new BsonDocument[]
            {
                new BsonDocument()
                {
                    {"$group", new BsonDocument()
                        {
                             {"_id", "$LevelNumeric" },
                             {"Count", new BsonDocument("$count", new BsonDocument()) }
                        }
                    }
                }
            };

            using IAsyncCursor<BsonDocument> cursor = await logCollection.AggregateAsync<BsonDocument>(stages, null, cancellationToken);

            Dictionary<LogLevel, double> logs = new Dictionary<LogLevel, double>()
            {
                { LogLevel.Critical, 0.0 },
                { LogLevel.Debug, 0.0 },
                { LogLevel.Error, 0.0 },
                { LogLevel.Information, 0.0 },
                { LogLevel.Trace, 0.0 },
                { LogLevel.Warning, 0.0 }
            };

            foreach (BsonDocument result in await cursor.ToListAsync(cancellationToken))
            {
                int levelValue = result["_id"].AsInt32;
                double value = this.ConvertBsonToDouble(result["Count"]);

                logs[(LogLevel)levelValue] = value;
            }

            decimal divider = (decimal)logs.Values.Sum();
            if (divider == 0.0M)
            {
                divider = 1.0M;
            }

            List<LogShare> logShares = new List<LogShare>();
            foreach ((LogLevel level, double count) in logs)
            {
                decimal percentage = (100.0M * (decimal)count) / divider;
                logShares.Add(new LogShare(level, percentage));
            }

            return logShares;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error during executionTime GetLevelsDistribution.");
            throw;
        }
    }

    public async Task<IReadOnlyList<ApplicationShare>> GetApplicationsDistribution(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetApplicationsDistribution.");

        const int GetMaxApplications = 8;

        try
        {
            IMongoCollection<MongoLogEntity> logCollection = this.mongoDatabase.GetCollection<MongoLogEntity>(CollectionNames.LogEntities);

            BsonDocument[] stages = new BsonDocument[]
            {
                new BsonDocument("$match", new BsonDocument()
                {
                    { "Properties.Name", "Application"}
                }),
                new BsonDocument("$project", new BsonDocument()
                {
                    { "Properties", new BsonDocument()
                    {
                        { "$filter", new BsonDocument()
                            {
                                { "input", "$Properties" },
                                { "as", "propObj" },
                                { "cond", new BsonDocument()
                                     {
                                         {"$eq", new BsonArray()
                                             {
                                                "$$propObj.Name",
                                                "Application"
                                             }
                                         }
                                     }
                                }
                            }
                        }
                     }
                }
                }),
                new BsonDocument("$project", new BsonDocument()
                {
                    { "Application", new BsonDocument()
                       {
                          {"$first", "$Properties.Values" }
                       }
                    }
                }),
                new BsonDocument("$group", new BsonDocument()
                {
                    { "_id", "$Application"},
                    { "Count", new BsonDocument()
                        {
                            {"$count", new BsonDocument() }
                        }
                    }
                }),
            };

            List<ApplicationShare> applicationCounts = new List<ApplicationShare>();
            using (IAsyncCursor<BsonDocument> cursor = await logCollection.AggregateAsync<BsonDocument>(stages, null, cancellationToken))
            {
                foreach (BsonDocument result in await cursor.ToListAsync(cancellationToken))
                {
                    string application = result["_id"].AsString;
                    double value = this.ConvertBsonToDouble(result["Count"]);

                    applicationCounts.Add(new ApplicationShare(application, (decimal)value));
                }

                applicationCounts.Sort((a, b) => b.Percent.CompareTo(a.Percent));
            }

            decimal totalCount = (decimal)await logCollection.CountDocumentsAsync(Builders<MongoLogEntity>.Filter.Empty, null, cancellationToken);

            List<ApplicationShare> listResult = new List<ApplicationShare>(GetMaxApplications);
            decimal nonOtherCount = 0.0M;

            foreach (ApplicationShare countShare in applicationCounts.Take(GetMaxApplications - 1))
            {
                nonOtherCount += countShare.Percent;
                listResult.Add(countShare with { Percent = (100.0M * countShare.Percent) / totalCount });
            }

            decimal otherCount = totalCount - nonOtherCount;
            if (otherCount > 0.0m)
            {
                listResult.Add(new ApplicationShare("Other...", (100.0m * otherCount) / totalCount));
            }

            return listResult;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error during executionTime GetApplicationsDistribution.");
            throw;
        }
    }

    private double ConvertBsonToDouble(BsonValue value)
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

        throw new InvalidProgramException("Bson value can not convert to double.");
    }
}
