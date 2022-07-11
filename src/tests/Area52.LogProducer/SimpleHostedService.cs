﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.LogProducer;

public class SimpleHostedService : BackgroundService
{
    private readonly ILogger<SimpleHostedService> logger;

    public SimpleHostedService(ILogger<SimpleHostedService> logger)
    {
        this.logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogInformation("Start hosted service");
        long run = 1L;

        while (!stoppingToken.IsCancellationRequested)
        {
            using IDisposable scope = this.logger.BeginScope("Run {0}", run++);
            int delay = Random.Shared.Next(500, 5 * 1000);
            this.logger.LogDebug("Start sleep to: {sleepTime} ms", delay);
            await Task.Delay(delay);

            this.logger.LogInformation("Used memory: {workingSet} with user: {user}.",
                Environment.WorkingSet,
                Environment.UserName);
        }
    }
}
