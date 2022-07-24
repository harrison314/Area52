using Area52.Infrastructure.App;
using Area52.Infrastructure.Clef;
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
                    .Enrich.FromLogContext()
                    .WriteTo.Console());


            IBackendConfigurator configurator = BackendConfiguratorFactory.Create(builder.Configuration);
            configurator.GlobalSetup();
            configurator.ConfigureServices(builder);

            builder.Services.AddDataProtection()
               // .ProtectKeysWithCertificate() TODO: implemet protection
               .ConfigureDataProtectionStorage(configurator);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();


            if (builder.Configuration.GetValue<bool>("ArchiveSettings:Enabled"))
            {
                builder.Services.AddHostedService<Area52.Infrastructure.HostedServices.ArchivationHostedService>();
            }

            builder.Services.AddHostedService<Area52.Infrastructure.HostedServices.StartupJobHostingService>();

            builder.Services.AddHealthChecks().ConfigureHealthChecks(configurator);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHealthChecks("/health");
            app.UseEventMiddleware();
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
