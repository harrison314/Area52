using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Infrastructure.App;
using Area52.Services.Configuration;
using Area52.Services.Contracts.Statistics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo.Infrastructure;

public class BackendConfigurator : IBackendConfigurator
{
    private readonly IFeatureManagement featureManagement;

    public BackendConfigurator(IFeatureManagement featureManagement)
    {
        this.featureManagement = featureManagement;
    }

    public void GlobalSetup()
    {
        Models.ModelHelpers.RegisterMappings();
    }

    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.Configure<MongoDbSetup>(builder.Configuration.GetSection("MongoDbSetup"));
        builder.Services.AddSingleton<IMongoClient>(sp =>
        {
            IOptions<MongoDbSetup> setup = sp.GetRequiredService<IOptions<MongoDbSetup>>();
            return new MongoClient(setup.Value.Database.ConnectionString);
        });

        builder.Services.AddSingleton<IMongoDatabase>(sp =>
        {
            IOptions<MongoDbSetup> setup = sp.GetRequiredService<IOptions<MongoDbSetup>>();
            IMongoClient client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(setup.Value.Database.DatabaseName);
        });

        builder.Services.AddTransient<Contracts.IStartupJob, MongoStartupJob>();
        this.RegisterServicesInternal(builder.Services);
    }

    private void RegisterServicesInternal(IServiceCollection services)
    {
        services.AddSingleton<Contracts.ILogWriter, LogWriter>();
        services.AddSingleton<Contracts.ILogReader, LogReader>();
        services.AddTransient<Contracts.ILogManager, LogManager>();
        services.AddTransient<Contracts.IDistributedLocker, MongoDbDistributedLocker>();

        if (this.featureManagement.IsFeatureEnabled(FeatureNames.TimeSeries))
        {
            services.AddTransient<Contracts.TimeSeries.ITimeSerieDefinitionsRepository, TimeSeries.TimeSerieDefinitionsRepository>();
            services.AddTransient<Contracts.TimeSeries.ITimeSeriesService, TimeSeries.TimeSeriesService>();
        }

        services.AddTransient<Contracts.Statistics.IFastStatisticsServices>(this.RegisterCachedFastStatisticServices);
        services.AddTransient<Contracts.IUserPrefernceServices, UserPrefernceServices>();
        services.AddTransient<Contracts.IApiKeyServices, ApiKeyServices>();
    }

    public void AddHealthChecks(IHealthChecksBuilder healthChecksBuilder)
    {
        healthChecksBuilder.AddCheck<HealthCheck.MongoDbHealthCheck>("MongoDb",
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
            null,
            TimeSpan.FromSeconds(10.0));
    }

    public void AddDataProtectionStorage(IDataProtectionBuilder dataProtectionBuilder)
    {
        DataProtection.MongoDbXmlRepositoryHelper.Register(dataProtectionBuilder);
    }

    private IFastStatisticsServices RegisterCachedFastStatisticServices(IServiceProvider sp)
    {
        IMongoDatabase mongoDatabase = sp.GetRequiredService<IMongoDatabase>();
        ILogger<Statistics.FastStatisticsServices> logger = sp.GetRequiredService<ILogger<Statistics.FastStatisticsServices>>();
        Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();

        Statistics.FastStatisticsServices fss = new Statistics.FastStatisticsServices(mongoDatabase, logger);
        return new FastStatisticsServicesCache(fss, memoryCache);
    }
}

