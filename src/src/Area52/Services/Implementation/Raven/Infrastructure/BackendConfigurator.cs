using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Area52.Infrastructure.App;
using Area52.Services.Configuration;
using Mcrio.AspNetCore.Identity.On.RavenDb;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.Role;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.User;
using Mcrio.AspNetCore.Identity.On.RavenDb.Stores;
using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Area52.Services.Implementation.Raven.Infrastructure;

public class BackendConfigurator : IBackendConfigurator
{
    public BackendConfigurator()
    {

    }

    public void GlobalSetup()
    {
        // NOP
    }

    public void ConfigureServices(WebApplicationBuilder builder)
    {
        RavenDbSetup settings = new RavenDbSetup();
        builder.Configuration.Bind("RavenDbSetup", settings);

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


        builder.Services.AddSingleton<IDocumentStore>(store);

        builder.Services.AddScoped<IAsyncDocumentSession>(serviceProvider =>
        {
            return serviceProvider
                .GetRequiredService<IDocumentStore>()
                .OpenAsyncSession();
        });

        builder.Services.AddTransient<Contracts.IStartupJob, RavenDbIndexJob>();

        this.RegisvicesInternal(builder.Services);
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

    private void RegisvicesInternal(IServiceCollection services)
    {
        services.AddTransient<Contracts.ILogReader, LogReader>();
        services.AddSingleton<Contracts.ILogWriter, LogWriter>();
        services.AddSingleton<Contracts.ILogManager, LogManager>();
        services.AddSingleton<Contracts.IDistributedLocker, DistributedLocker>();
    }
}
