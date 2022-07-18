using Area52.Infrastructure.Clef;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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


            builder.Services.Configure<Area52.Services.Configuration.ArchiveSettings>(builder.Configuration.GetSection("ArchiveSettings"));


            Services.Configuration.LogStorageType storageType = builder.Configuration.GetValue<Services.Configuration.LogStorageType>("StorageType");

            switch (storageType)
            {
                case Services.Configuration.LogStorageType.RavenDb:
                    Area52.Services.Implementation.Raven.RavenDbExtensions.AddRavenDb(builder);
                    break;

                case Services.Configuration.LogStorageType.MongoDb:
                    Area52.Services.Implementation.Mongo.MongoDbExtensions.AddMongoDb(builder);
                    break;

                default:
                    throw new InvalidProgramException($"Enum value {storageType} is not supported.");
            }



            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();


            if (builder.Configuration.GetValue<bool>("ArchiveSettings:Enabled"))
            {
                builder.Services.AddHostedService<Area52.Infrastructure.HostedServices.ArchivationHostedService>();
            }

            builder.Services.AddHostedService<Area52.Infrastructure.HostedServices.StartupJobHostingService>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

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
