using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Area52.Infrastructure.App;

public interface IBackendConfigurator
{
    void GlobalSetup();

    void ConfigureServices(WebApplicationBuilder builder);

    void AddHealthChecks(IHealthChecksBuilder healthChecksBuilder);

    void ConfigureIdentity(WebApplicationBuilder builder, Action<IdentityOptions> identityOptions);
}
