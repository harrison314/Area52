using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;

namespace Area52.Infrastructure.HostedServices;

public class StartupJobHostingService : IHostedService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<StartupJobHostingService> logger;

    public StartupJobHostingService(IServiceProvider serviceProvider, ILogger<StartupJobHostingService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("entering to StartAsync.");
        using IServiceScope scope = this.serviceProvider.CreateScope();

        IEnumerable<IStartupJob> jobs = scope.ServiceProvider.GetRequiredService<IEnumerable<IStartupJob>>();
        string jobTypeName = string.Empty;
        try
        {
            foreach (IStartupJob job in jobs)
            {
                jobTypeName = job.GetType().FullName!;
                this.logger.LogTrace("Starting executing job {jobName}.", jobTypeName);
                await job.Execute(cancellationToken);
                this.logger.LogDebug("Executed job {jobName} successfully.", jobTypeName);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogCritical(ex, "Fatal error during startup job {jobName}.", jobTypeName);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
