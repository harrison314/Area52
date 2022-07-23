using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Infrastructure.App;

namespace Area52.Services.Implementation.Mongo.Infrastructure;

public class BackendConfigurator : IBackendConfigurator
{
    public BackendConfigurator()
    {

    }

    public void GlobalSetup()
    {
        Models.ModelHelpers.RegisterMappings();
    }

    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.AddMongoDb();
    }
}

