using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Area52.Infrastructure.App;
using Area52.Services.Configuration;
using Area52.Services.Contracts.TimeSeries;
using Area52.Services.Implementation.Raven.TimeSeries;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Microsoft.AspNetCore.Identity;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.User;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.Role;
using Mcrio.AspNetCore.Identity.On.RavenDb.Stores;
using Mcrio.AspNetCore.Identity.On.RavenDb;

namespace Area52.Services.Implementation.Raven.Infrastructure;

public class BackendConfigurator : IBackendConfigurator
{
    private readonly IFeatureManagement featureManagement;

    public BackendConfigurator(IFeatureManagement featureManagement)
    {
        this.featureManagement = featureManagement;
    }

    public void GlobalSetup()
    {
        // NOP
    }

    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.Configure<RavenDbSetup>(builder.Configuration.GetSection("RavenDbSetup"));
        builder.Services.AddSingleton<IDocumentStore>(sp =>
        {
            IOptions<RavenDbSetup> ravenDbSetup = sp.GetRequiredService<IOptions<RavenDbSetup>>();
            RavenDbSetup settings = ravenDbSetup.Value;

            DocumentStore store = new DocumentStore()
            {
                Urls = settings.Database.Urls,
                Database = settings.Database.DatabaseName,

            };

            if (!string.IsNullOrEmpty(settings.Database.CertPath))
            {
                store.Certificate = new X509Certificate2(settings.Database.CertPath, settings.Database.CertPass);
            }

            store.Initialize();

            return store;
        });

        builder.Services.AddScoped<IAsyncDocumentSession>(serviceProvider =>
        {
            return serviceProvider
                .GetRequiredService<IDocumentStore>()
                .OpenAsyncSession();
        });

        builder.Services.AddTransient<Contracts.IStartupJob, RavenDbIndexJob>();
        this.RegisterServicesInternal(builder.Services);
    }

    private void RegisterServicesInternal(IServiceCollection services)
    {
        services.AddSingleton<Contracts.ILogReader, LogReader>();
        services.AddSingleton<Contracts.ILogWriter, LogWriter>();
        services.AddSingleton<Contracts.ILogManager, LogManager>();
        services.AddSingleton<Contracts.ILogDistributionServices, LogDistributionServices>();
        services.AddSingleton<Contracts.IDistributedLocker, DistributedLocker>();

        if (this.featureManagement.IsFeatureEnabled(FeatureNames.TimeSeries))
        {
            services.AddSingleton<ITimeSerieDefinitionsRepository, TimeSerieDefinitionsRepository>();
            services.AddSingleton<ITimeSeriesService, TimeSeriesService>();
        }

        services.AddSingleton<Contracts.IUserPrefernceServices, UserPrefernceServices>();
        services.AddSingleton<Contracts.IApiKeyServices, ApiKeyServices>();

        services.AddSingleton<Contracts.Statistics.IFastStatisticsServices>(this.RegisterCachedFastStatisticServices);
    }

    public void AddHealthChecks(IHealthChecksBuilder healthChecksBuilder)
    {
        healthChecksBuilder.AddCheck<HealthCheck.RavenDbHealthCheck>("RavenDb",
             Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
             null,
             TimeSpan.FromSeconds(10.0));
    }

    public void AddDataProtectionStorage(IDataProtectionBuilder dataProtectionBuilder)
    {
        DataProtection.RavenXmlRepositoryHelper.Register(dataProtectionBuilder);
    }

    private Contracts.Statistics.IFastStatisticsServices RegisterCachedFastStatisticServices(IServiceProvider sp)
    {
        IDocumentStore documentStore = sp.GetRequiredService<IDocumentStore>();
        ILogger<Statistics.FastStatisticsServices> logger = sp.GetRequiredService<ILogger<Statistics.FastStatisticsServices>>();
        Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();

        Statistics.FastStatisticsServices fss = new Statistics.FastStatisticsServices(documentStore, logger);
        return new FastStatisticsServicesCache(fss, memoryCache);
    }

    public void ConfigureIdentity(WebApplicationBuilder builder, Action<IdentityOptions> identityOptions)
    {
        builder.Services.AddIdentity<RavenIdentityUser, RavenIdentityRole>(identityOptions)
            .AddRavenDbStores<RavenUserStore, RavenRoleStore, RavenIdentityUser, RavenIdentityRole>(
        // define how IAsyncDocumentSession is resolved from DI
        // as library does NOT directly inject IAsyncDocumentSession
        provider => provider.GetRequiredService<IAsyncDocumentSession>()
    )
    .AddDefaultTokenProviders();

        builder.Services.AddTransient<Services.Contracts.IUserServices, UserService>();
    }
}
