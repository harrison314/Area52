using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Area52.Infrastructure.App;
using Area52.Services.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
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
        this.RegisvicesInternal(builder.Services);
    }

    private void RegisvicesInternal(IServiceCollection services)
    {
        services.AddTransient<Contracts.ILogReader, LogReader>();
        services.AddSingleton<Contracts.ILogWriter, LogWriter>();
        services.AddSingleton<Contracts.ILogManager, LogManager>();
        services.AddSingleton<Contracts.IDistributedLocker, DistributedLocker>();
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
}
