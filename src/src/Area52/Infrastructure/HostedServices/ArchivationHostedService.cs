using Area52.Services.Configuration;
using Area52.Services.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Infrastructure.HostedServices;

public class ArchivationHostedService : BackgroundService
{
    private readonly ILogManager logManager;
    private readonly IDistributedLocker locker;
    private readonly IOptions<ArchiveSettings> archivationSettings;
    private readonly ILogger<ArchivationHostedService> logger;

    public ArchivationHostedService(ILogManager logManager,
        IDistributedLocker locker,
        IOptions<ArchiveSettings> archivationSettings,
        ILogger<ArchivationHostedService> logger)
    {
        this.logManager = logManager;
        this.locker = locker;
        this.archivationSettings = archivationSettings;
        this.logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogTrace("Entering to ExecuteAsync.");
        if (!this.archivationSettings.Value.Enabled)
        {
            return;
        }

        PeriodicTimer timer = new PeriodicTimer(this.archivationSettings.Value.CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await using IDistributedLock dLock = await this.locker.TryAquire("LogErasing",
                this.archivationSettings.Value.CheckInterval.Add(TimeSpan.FromSeconds(60)),
                stoppingToken);

            if (dLock.Aquired)
            {
                try
                {
                    DateTimeOffset date = DateTimeOffset.UtcNow - TimeSpan.FromDays(this.archivationSettings.Value.RemovaLogsAdDaysOld);
                    await this.logManager.RemoveOldLogs(date, stoppingToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error during removing logs.");
                }

                await timer.WaitForNextTickAsync(stoppingToken);
            }
            else
            {
                await Task.Delay(TimeSpan.FromMinutes(1.0), stoppingToken);
            }

        }
    }
}
