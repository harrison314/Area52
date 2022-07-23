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
        this.logger.LogTrace("Entering to StartAsync.");
        //await this.WaitForAppStartup(cancellationToken);
        await Task.Delay(200);

        using IServiceScope scope = this.serviceProvider.CreateScope();

        IEnumerable<IStartupJob> jobs = scope.ServiceProvider.GetRequiredService<IEnumerable<IStartupJob>>();
        string jobTypeName = string.Empty;
        try
        {
            foreach (IStartupJob job in jobs)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

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

    private async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
    {
        var startedSource = new TaskCompletionSource();
        var cancelledSource = new TaskCompletionSource();

        using var reg1 = lifetime.ApplicationStarted.Register(() => startedSource.SetResult());
        using var reg2 = stoppingToken.Register(() => cancelledSource.SetResult());

        Task completedTask = await Task.WhenAny(
            startedSource.Task,
            cancelledSource.Task).ConfigureAwait(false);

        // If the completed tasks was the "app started" task, return true, otherwise false
        return completedTask == startedSource.Task;
    }

    private Task<bool> WaitForAppStartup(CancellationToken stoppingToken)
    {
        IHostApplicationLifetime lifetime = this.serviceProvider.GetRequiredService<IHostApplicationLifetime>();
        return this.WaitForAppStartup(lifetime, stoppingToken);
    }
}
