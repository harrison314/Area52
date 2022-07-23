using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Infrastructure.App;
using Microsoft.AspNetCore.Identity;

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
        builder.AddRavenDb();
    }

    public void ConfigureIdentity(WebApplicationBuilder builder, Action<IdentityOptions> identityOptions)
    {
        throw new NotImplementedException();
    }
}
