using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Infrastructure.App;

public interface IBackendConfigurator
{
    void GlobalSetup();

    void ConfigureServices(WebApplicationBuilder builder);
}
