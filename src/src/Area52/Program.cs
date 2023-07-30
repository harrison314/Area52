using Area52.Infrastructure.App;
using Area52.Infrastructure.Clef;
using Area52.Services.Contracts.TimeSeries;
using BlazorStrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Serilog;

namespace Area52
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Serilog.Debugging.SelfLog.Enable(Console.Error);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext());


            builder.Services.Configure<Services.Configuration.Area52Setup>(builder.Configuration.GetSection(nameof(Services.Configuration.Area52Setup)));
            IFeatureManagement featureManagement = builder.AddFeatureManagement();

            IBackendConfigurator configurator = BackendConfiguratorFactory.Create(builder.Configuration, featureManagement);
            configurator.GlobalSetup();
            configurator.ConfigureServices(builder);

            builder.Services.AddDataProtection()
               // .ProtectKeysWithCertificate() TODO: implemet protection
               .ConfigureDataProtectionStorage(configurator);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddBlazorStrap();

            builder.Services.AddMemoryCache();

            if (featureManagement.IsFeatureEnabled(FeatureNames.BackgroundProcessing))
            {
                if (builder.Configuration.GetValue<bool>("ArchiveSetup:Enabled"))
                {
                    builder.Services.Configure<Services.Configuration.ArchiveSetup>(builder.Configuration.GetSection("ArchiveSetup"));
                    builder.Services.AddHostedService<Area52.Infrastructure.HostedServices.ArchivationHostedService>();
                }

                builder.Services.AddHostedService<Area52.Infrastructure.HostedServices.StartupJobHostingService>();

                builder.Services.Configure<Services.Configuration.TimeSeriesSetup>(builder.Configuration.GetSection("TimeSeriesSetup"));

                if (featureManagement.IsFeatureEnabled(FeatureNames.TimeSeries))
                {
                    builder.Services.AddHostedService<Area52.Infrastructure.HostedServices.TimeSeriesBackgroundService>();
                }
            }

            // Services
            if (featureManagement.IsFeatureEnabled(FeatureNames.TimeSeries))
            {
                builder.Services.AddTransient<ITimeSerieDefinitionsService, Services.Implementation.TimeSerieDefinitionsService>();
            }

            builder.Services.AddHealthChecks().ConfigureHealthChecks(configurator);

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            });

            var app = builder.Build();

            app.UseForwardedHeaders();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHealthChecks("/health");
            if (featureManagement.IsFeatureEnabled(FeatureNames.ClefEndpoint))
            {
                app.UseEventMiddleware();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");
            app.MapDownloadClefFile();
            app.Run();
        }
    }
}
