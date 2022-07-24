using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Infrastructure.App;

internal static class BackendConfiguratorExtensions
{
    public static IHealthChecksBuilder ConfigureHealthChecks(this IHealthChecksBuilder builder, IBackendConfigurator configurator)
    {
        configurator.AddHealthChecks(builder);
        return builder;
    }
}
