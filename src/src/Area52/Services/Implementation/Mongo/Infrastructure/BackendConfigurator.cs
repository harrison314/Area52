using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Infrastructure.App;
using Area52.Services.Configuration;
using AspNetCore.Identity.Mongo;
using AspNetCore.Identity.Mongo.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo.Infrastructure;

public class BackendConfigurator : IBackendConfigurator
{
    private MongoDbSetup? setup;

    public BackendConfigurator()
    {
        this.setup = null;
    }

    public void GlobalSetup()
    {
        Models.ModelHelpers.RegisterMappings();
    }

    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.Configure<MongoDbSetup>(builder.Configuration.GetSection("MongoDbSetup"));
        this.setup = builder.Configuration.GetSection("MongoDbSetup").Get<MongoDbSetup>();

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

    public void ConfigureIdentity(WebApplicationBuilder builder, Action<IdentityOptions> identityOptions)
    {
        if (this.setup == null)
        {
            throw new InvalidOperationException();
        }

        builder.Services.AddIdentity<MongoUser<string>, MongoRole<string>>(identityOptions)
        .AddMongoDbStores<MongoUser<string>, MongoRole<string>, string>(mongo =>
        {
            mongo.ConnectionString = this.BuildConnectionString(this.setup);
            mongo.MigrationCollection = "_IdentityMigrations";
            mongo.UsersCollection = "IdentityUsers";
            mongo.RolesCollection = "IdentityRoles";
        })
        .AddDefaultTokenProviders();

        builder.Services.AddTransient<Contracts.IUserServices, UserService>();
    }

    private string BuildConnectionString(MongoDbSetup setup)
    {
        string baseAdress = setup.Database.ConnectionString.TrimEnd('/');
        return string.Concat(baseAdress, "/", setup.Database.DatabaseName);
    }

    public void AddHealthChecks(IHealthChecksBuilder healthChecksBuilder)
    {
        healthChecksBuilder.AddCheck<HealthCheck.MongoDbHealthCheck>("MongoDb",
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
            null,
            TimeSpan.FromSeconds(10.0));
    }
}

