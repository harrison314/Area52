using Area52.Infrastructure.App;
using Area52.Infrastructure.Clef;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
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

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();


            if (builder.Configuration.GetValue<bool>("ArchiveSettings:Enabled"))
            {
                builder.Services.AddHostedService<Area52.Infrastructure.HostedServices.ArchivationHostedService>();
            }

            builder.Services.AddHostedService<Area52.Infrastructure.HostedServices.StartupJobHostingService>();


            configurator.ConfigureIdentity(builder, options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            });

            builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, Infrastructure.Auth.RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
            builder.Services.AddTransient<Services.Contracts.IStartupJob, Infrastructure.Auth.AuthStartupJob>();

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
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");
            app.MapDownloadClefFile();
            app.Run();
        }
    }
}
