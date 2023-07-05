using System;
using System.Net.Http;
using Area52.Ufo.Services.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Polly.Extensions.Http;
using Polly;
using Serilog;

namespace Area52.Ufo;

public static class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
          .MinimumLevel.Verbose()
      .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
      .Enrich.FromLogContext()
      .WriteTo.Console()
      .CreateBootstrapLogger();

        WebApplicationOptions webApplicationOptions = new WebApplicationOptions()
        {
            Args = args,
            ContentRootPath = WindowsServiceHelpers.IsWindowsService()
                                 ? AppContext.BaseDirectory : default
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder(webApplicationOptions);
        builder.Host.UseSerilog((context, services, configuration) => configuration
              .ReadFrom.Configuration(context.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .WriteTo.Console());

        builder.Host.UseWindowsService(serviceOptions =>
        {
            serviceOptions.ServiceName = builder.Configuration.GetValue<string?>("WindowsServiceName", null);
        });

        builder.Services.Configure<UfoSetup>(builder.Configuration.GetSection(nameof(UfoSetup)));
        builder.Services.Configure<FolderWatchSetup>(builder.Configuration.GetSection(nameof(FolderWatchSetup)));
        builder.Services.Configure<SyslogSetup>(builder.Configuration.GetSection(nameof(SyslogSetup)));

        builder.Services.AddHttpClient<Services.Implementation.BatchClefQeueu>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5.0))  //Set lifetime to five minutes
            .AddPolicyHandler(GetRetryPolicy());

        builder.Services.AddSingleton<Services.Contracts.IBatchClefClient, Services.Implementation.BatchClefClient>();

        if (builder.Configuration.GetValue<bool>("FolderWatchSetup:Enabled", false))
        {
            builder.Services.AddHostedService<Infrastructure.HostedServices.WatchFolderHostedService>();
        }

        if (builder.Configuration.GetValue<bool>("SyslogSetup:Enabled", false))
        {
            builder.Services.AddHostedService<Infrastructure.HostedServices.SyslogHostedService>();
        }

        if (builder.Configuration.GetValue<bool>("SyslogSetup:Enabled", false) /*Other conditions */)
        {
            builder.Services.AddSingleton<Services.Contracts.IBatchClefQeueu, Services.Implementation.BatchClefQeueu>();
            builder.Services.AddHostedService<Infrastructure.HostedServices.SendingHostedService>();
        }

        builder.Services.AddHealthChecks(); //TODO: Add Health Checks

        WebApplication app = builder.Build();

        app.MapHealthChecks("/health");
        app.MapGet("/", () => "Hello from Area52.Ufo!");

        app.Run();
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2.0, retryAttempt)));
    }
}
