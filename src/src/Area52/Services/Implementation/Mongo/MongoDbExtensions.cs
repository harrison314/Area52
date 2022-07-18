using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo;

public static class MongoDbExtensions
{
    public static void AddMongoDb(WebApplicationBuilder builder)
    {

        Models.ModelHelpers.RegisterMappings();

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
        builder.Services.AddSingleton<Contracts.ILogWriter, LogWriter>();
        builder.Services.AddSingleton<Contracts.ILogReader, LogReader>();
        builder.Services.AddTransient<Contracts.ILogManager, LogManager>();
        builder.Services.AddTransient<Contracts.IDistributedLocker, MongoDbDistributedLocker>();
    }
}
