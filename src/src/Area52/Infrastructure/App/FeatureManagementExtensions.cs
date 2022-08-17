using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Infrastructure.App;

internal static class FeatureManagementExtensions
{
    public static IFeatureManagement AddFeatureManagement(this WebApplicationBuilder? builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigFeatureManagement featureManagement = new ConfigFeatureManagement(builder.Configuration.GetSection("Area52Setup:Features"));
        builder.Services.AddSingleton<IFeatureManagement>(featureManagement);

        return featureManagement;
    }
}
