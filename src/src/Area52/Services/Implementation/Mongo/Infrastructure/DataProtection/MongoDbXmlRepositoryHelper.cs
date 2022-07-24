using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo.Infrastructure.DataProtection;

internal static class MongoDbXmlRepositoryHelper
{
    public static void Register(IDataProtectionBuilder dataProtectionBuilder)
    {
        dataProtectionBuilder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            IMongoDatabase documentSorage = services.GetRequiredService<IMongoDatabase>();
            ILogger<MongoDbXmlRepository> logger = services.GetRequiredService<ILogger<MongoDbXmlRepository>>();

            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new MongoDbXmlRepository(documentSorage, logger);
            });
        });
    }
}
