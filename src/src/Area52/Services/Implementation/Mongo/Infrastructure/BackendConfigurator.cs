using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Infrastructure.App;
using Area52.Services.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo.Infrastructure;

public class BackendConfigurator : IBackendConfigurator
{
    public BackendConfigurator()
    {

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
        this.RegisvicesInternal(builder.Services);
    }

    private void RegisvicesInternal(IServiceCollection services)
    {
        services.AddSingleton<Contracts.ILogWriter, LogWriter>();
        services.AddSingleton<Contracts.ILogReader, LogReader>();
        services.AddTransient<Contracts.ILogManager, LogManager>();
        services.AddTransient<Contracts.IDistributedLocker, MongoDbDistributedLocker>();
    }
}

