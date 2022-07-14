using Area52.Services.Configuration;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Implementation.Raven;

public static class RavenDbExtensions
{
    public static void AddRavenDb(this WebApplicationBuilder? builder)
    {
        System.Diagnostics.Debug.Assert(builder != null);

        var settings = new RavenDbSetup();
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

        RegisvicesInternal(builder.Services);
    }

    private static void RegisvicesInternal(IServiceCollection services)
    {
        services.AddTransient<Contracts.ILogReader, LogReader>();
        services.AddSingleton<Contracts.ILogWriter, LogWriter>();
        services.AddSingleton<Contracts.ILogManager, LogManager>();
        services.AddSingleton<Contracts.IDistributedLocker, DistributedLocker>();
    }
}
