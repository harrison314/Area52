using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Configuration;

namespace Area52.Infrastructure.App;

internal static class BackendConfiguratorFactory
{
    public static IBackendConfigurator Create(IConfiguration configuration, IFeatureManagement featureManagement)
    {
        LogStorageType storageType = configuration.GetValue<LogStorageType>("StorageType");

        return storageType switch
        {
            LogStorageType.RavenDb => new Area52.Services.Implementation.Raven.Infrastructure.BackendConfigurator(featureManagement),
            LogStorageType.MongoDb => new Area52.Services.Implementation.Mongo.Infrastructure.BackendConfigurator(featureManagement),
            _ => throw new InvalidProgramException($"Enum value {storageType} is not supported.")
        };
    }
}
