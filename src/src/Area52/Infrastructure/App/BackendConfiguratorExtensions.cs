using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;

namespace Area52.Infrastructure.App;

internal static class BackendConfiguratorExtensions
{
    public static IHealthChecksBuilder ConfigureHealthChecks(this IHealthChecksBuilder builder, IBackendConfigurator configurator)
    {
        configurator.AddHealthChecks(builder);
        return builder;
    }

    public static IDataProtectionBuilder ConfigureDataProtectionStorage(this IDataProtectionBuilder dataProtectionBuilder, IBackendConfigurator configurator)
    {
        configurator.AddDataProtectionStorage(dataProtectionBuilder);
        return dataProtectionBuilder;
    }
}
