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

    public async Task<BaseStatistics> GetBaseStatistics(CancellationToken cancellationToken)
    {
        BaseStatistics? baseStatistics = await this.memoryCache.GetOrCreateAsync("IFastStatisticsServices:GetBaseStatistics", (entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2.0);
            return this.parent.GetBaseStatistics(CancellationToken.None);
        });

        if (baseStatistics == null)
        {
            throw new InvalidOperationException("Can not crated IFastStatisticsServices:GetBaseStatistics");
        }

        return baseStatistics;
    }

    public async Task<IReadOnlyList<ApplicationShare>> GetApplicationsDistribution(CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationShare>? applicationShares = await this.memoryCache.GetOrCreateAsync("IFastStatisticsServices:GetApplicationsDistribution", (entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5.0);
            return this.parent.GetApplicationsDistribution(CancellationToken.None);
        });

        if (applicationShares == null)
        {
            throw new InvalidOperationException("Can not crated IFastStatisticsServices:GetApplicationsDistribution");
        }

        return applicationShares;
    }

    public async Task<IReadOnlyList<LogShare>> GetLevelsDistribution(CancellationToken cancellationToken)
    {
        IReadOnlyList<LogShare>? levelDistributiobs = await this.memoryCache.GetOrCreateAsync("IFastStatisticsServices:GetLevelsDistribution", (entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5.0);
            return this.parent.GetLevelsDistribution(CancellationToken.None);
        });

        if (levelDistributiobs == null)
        {
            throw new InvalidOperationException("Can not crated IFastStatisticsServices:GetLevelsDistribution");
        }

        return levelDistributiobs;
    }
}
