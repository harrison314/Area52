using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo.Infrastructure.HealthCheck;

public class MongoDbHealthCheck : IHealthCheck
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<MongoDbHealthCheck> logger;

    public MongoDbHealthCheck(IServiceProvider serviceProvider, ILogger<MongoDbHealthCheck> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        this.logger.LogTrace("Enterin to CheckHealthAsync.");

        try
        {
            IMongoDatabase mongoDatabase = this.serviceProvider.GetRequiredService<IMongoDatabase>();
            await mongoDatabase.RunCommandAsync<BsonDocument>((Command<BsonDocument>)new BsonDocument("ping", 1), null, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            this.logger.LogCritical(ex, "Error during MongoDB check health.");
            return HealthCheckResult.Unhealthy("MongoDb failed.", ex);
        }
    }
}
