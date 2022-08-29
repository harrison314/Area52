using Area52.Ufo.Services.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Area52.Ufo
{
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

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog((context, services, configuration) => configuration
                  .ReadFrom.Configuration(context.Configuration)
                  .ReadFrom.Services(services)
                  .Enrich.FromLogContext()
                  .WriteTo.Console());

            builder.Services.Configure<UfoSetup>(builder.Configuration.GetSection(nameof(UfoSetup)));
            builder.Services.Configure<FolderWatchSetup>(builder.Configuration.GetSection(nameof(FolderWatchSetup)));
            builder.Services.Configure<SyslogSetup>(builder.Configuration.GetSection(nameof(SyslogSetup)));

            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<Services.Contracts.IBatchClefClient, Services.Implementation.BatchClefClient>();

            if (builder.Configuration.GetValue<bool>("FolderWatchSetup:Enabled", false))
            {
                builder.Services.AddHostedService<Infrastructure.HostedServices.WatchFolderHostedService>();
            }

            builder.Services.AddHealthChecks(); //TODO: Add Health Checks

            WebApplication app = builder.Build();

            app.MapHealthChecks("/health");
            app.MapGet("/", () => "Hello from Area52.Ufo!");

            app.Run();
        }
    }
}
