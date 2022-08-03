using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raven.Client.Documents;

namespace Area52.Services.Implementation.Raven.Infrastructure.HealthCheck;

public class RavenDbHealthCheck : IHealthCheck
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<RavenDbHealthCheck> logger;

    public RavenDbHealthCheck(IServiceProvider serviceProvider, ILogger<RavenDbHealthCheck> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        this.logger.LogTrace("Entering to CheckHealthAsync.");

        try
        {
            IDocumentStore documentStre = this.serviceProvider.GetRequiredService<IDocumentStore>();
            DatabaseHealthCheckOperation operation = new DatabaseHealthCheckOperation(context.Registration.Timeout);

            await documentStre.Operations.SendAsync(operation, token: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            this.logger.LogCritical(ex, "Error during RavenDb check health.");
            return HealthCheckResult.Unhealthy("RavenDb failed.", ex);
        }
    }
}
