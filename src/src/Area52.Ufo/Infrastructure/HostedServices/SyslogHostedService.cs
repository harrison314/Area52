using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Area52.Ufo.Services.Configuration;
using Area52.Ufo.Services.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyslogDecode.Model;
using SyslogDecode.Udp;

namespace Area52.Ufo.Infrastructure.HostedServices;

public class SyslogHostedService : IHostedService, IDisposable
{
    private bool disposedValue;
    private readonly IBatchClefQeueu batchClefQeueu;
    private readonly IOptions<SyslogSetup> syslogSetup;
    private readonly ILogger<SyslogHostedService> logger;

    private readonly SyslogUdpPipeline pipeline;

    public SyslogHostedService(IBatchClefQeueu batchClefQeueu,
        IOptions<SyslogSetup> syslogSetup,
        ILogger<SyslogHostedService> logger)
    {
        this.batchClefQeueu = batchClefQeueu;
        this.syslogSetup = syslogSetup;
        this.logger = logger;
        this.disposedValue = false;

        this.pipeline = this.CreatePipeline(syslogSetup.Value);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to StartAsync.");

        this.pipeline.StreamParser.ItemProcessed += this.StreamParser_ItemProcessed;
        this.pipeline.Start();

        this.logger.LogInformation("Open UDP Syslog listening on {syslogHost}:{syslogPort}",
            this.syslogSetup.Value.Host,
            this.syslogSetup.Value.Port);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to StopAsync.");

        try
        {
            this.pipeline.Stop();
            this.pipeline.StreamParser.ItemProcessed -= this.StreamParser_ItemProcessed;
            this.logger.LogInformation("Stop syslog listener.");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error during stopping.");
        }

        return Task.CompletedTask;
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~SerilogHostedService()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private SyslogUdpPipeline CreatePipeline(SyslogSetup configuration)
    {
        if (!IPAddress.TryParse(configuration.Host, out IPAddress? ipAddress))
        {
            IPHostEntry ipHostEntry = Dns.GetHostEntry(configuration.Host);
            ipAddress = ipHostEntry.AddressList.First();
        }

        return new SyslogUdpPipeline(ipAddress, configuration.Port);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (disposing)
            {
                this.pipeline.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            this.disposedValue = true;
        }
    }

    private void StreamParser_ItemProcessed(object? sender, SyslogDecode.Common.ItemEventArgs<SyslogDecode.Model.ParsedSyslogMessage> e)
    {
        try
        {
            ClefObject clefObject = new ClefObject()
            {
                Timestamp = (e.Item.Header.Timestamp.HasValue) ? e.Item.Header.Timestamp.Value : DateTimeOffset.UtcNow,
                Exception = null,
                EventId = null,
                Message = e.Item.Message,
                MessageTemplate = null,
                Renderings = null,
                Level = this.TranslateLevel(e.Item.Severity)
            };

            clefObject.Properties.TryAdd("Application", new ClefValue(e.Item.Header.AppName));
            clefObject.Properties.TryAdd("HostName", new ClefValue(e.Item.Header.HostName));

            if (e.Item.Header.ProcId != null && int.TryParse(e.Item.Header.ProcId, out int procId))
            {
                clefObject.Properties.TryAdd("ProcId", new ClefValue(procId));
            }

            foreach (NameValuePair item in e.Item.ExtractedTuples)
            {
                clefObject.Properties.TryAdd(item.Name, ClefValue.FromObject(item.Value));
            }

            this.ApplyConfigProperties(clefObject.Properties);

            this.batchClefQeueu.Insert(clefObject);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error during processing syslog message in StreamParser_ItemProcessed.");
        }
    }

    private void ApplyConfigProperties(Dictionary<string, object> properties)
    {
        foreach (AdditionalLogProperty addProperty in this.syslogSetup.Value.AdditionalPropertys)
        {
            if (addProperty.Override)
            {
                if (addProperty.Value == null)
                {
                    properties.Remove(addProperty.Name);
                }
                else
                {
                    properties[addProperty.Name] = ClefValue.FromObject(addProperty.Value);
                }
            }
            else
            {
                if (addProperty.Value != null && !properties.ContainsKey(addProperty.Name))
                {
                    properties.Add(addProperty.Name, ClefValue.FromObject(addProperty.Value));
                }
            }
        }
    }

    private string TranslateLevel(Severity severity)
    {
        return severity switch
        {
            Severity.Warning => nameof(LogLevel.Warning),
            Severity.Emergency => nameof(LogLevel.Critical),
            Severity.Alert => nameof(LogLevel.Error),
            Severity.Critical => nameof(LogLevel.Critical),
            Severity.Debug => nameof(LogLevel.Debug),
            Severity.Error => nameof(LogLevel.Error),
            Severity.Informational => nameof(LogLevel.Information),
            Severity.Notice => nameof(LogLevel.Warning),
            _ => throw new InvalidProgramException($"Invalid enum value {severity}.")
        };
    }
}
