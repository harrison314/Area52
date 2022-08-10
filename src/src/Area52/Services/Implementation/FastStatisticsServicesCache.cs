using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts.Statistics;
using Microsoft.Extensions.Caching.Memory;

namespace Area52.Services.Implementation;

public class FastStatisticsServicesCache : IFastStatisticsServices
{
    private readonly IFastStatisticsServices parent;
    private readonly IMemoryCache memoryCache;

    public FastStatisticsServicesCache(IFastStatisticsServices parent, IMemoryCache memoryCache)
    {
        this.parent = parent;
        this.memoryCache = memoryCache;
    }

    public Task<BaseStatistics> GetBaseStatistics(CancellationToken cancellationToken)
    {
        return this.memoryCache.GetOrCreateAsync("IFastStatisticsServices:GetBaseStatistics", (entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2.0);
            return this.parent.GetBaseStatistics(CancellationToken.None);
        });
    }

    public Task<IReadOnlyList<ApplicationShare>> GetApplicationsDistribution(CancellationToken cancellationToken)
    {
        return this.memoryCache.GetOrCreateAsync("IFastStatisticsServices:GetApplicationsDistribution", (entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5.0);
            return this.parent.GetApplicationsDistribution(CancellationToken.None);
        });
    }

    public Task<IReadOnlyList<LogShare>> GetLevelsDistribution(CancellationToken cancellationToken)
    {
        return this.memoryCache.GetOrCreateAsync("IFastStatisticsServices:GetLevelsDistribution", (entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5.0);
            return this.parent.GetLevelsDistribution(CancellationToken.None);
        });
    }
}
