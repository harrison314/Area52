using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;

namespace Area52.Services.Implementation.Raven.Infrastructure.DataProtection;

internal static class RavenXmlRepositoryHelper
{
    public static void Register(IDataProtectionBuilder dataProtectionBuilder)
    {
        dataProtectionBuilder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            IDocumentStore documentStorage = services.GetRequiredService<IDocumentStore>();
            ILogger<RavenXmlRepository> logger = services.GetRequiredService<ILogger<RavenXmlRepository>>();

            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new RavenXmlRepository(documentStorage, logger);
            });
        });
    }
}
