using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Infrastructure.App;

public class ConfigFeatureManagement : IFeatureManagement
{
    private readonly IConfigurationSection configurationSection;

    public ConfigFeatureManagement(IConfigurationSection configurationSection)
    {
        this.configurationSection = configurationSection;
    }

    public bool IsFeatureEnabled(string name)
    {
        return this.configurationSection.GetValue<bool>(name, false);
    }
}
