using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Area52.Ufo.Services.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Area52.Ufo.Infrastructure.HostedServices;

public class SendingHostedService : BackgroundService
{
    private readonly IBatchClefClient batchClefClient;
    private readonly IBatchClefQeueu batchClefQeueu;
    private readonly ILogger<SendingHostedService> logger;

    public SendingHostedService(IBatchClefClient batchClefClient,
        IBatchClefQeueu batchClefQeueu,
        ILogger<SendingHostedService> logger)
    {
        this.batchClefClient = batchClefClient;
        this.batchClefQeueu = batchClefQeueu;
        this.logger = logger;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogTrace("Entering to ExecuteAsync.");

        await Task.Delay(200);

        while (await this.batchClefQeueu.Wait(stoppingToken))
        {
            try
            {
                string clefContent = this.batchClefQeueu.GetContent();
                await this.batchClefClient.DirectSend(clefContent, CancellationToken.None);
            }
            catch(Exception ex)
            {
                this.logger.LogError(ex, "Error in sending loop.");
            }
        }
    }
}
