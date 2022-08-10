using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Area52.Services.Contracts.Statistics;
using Area52.Services.Implementation.Raven.Indexes;
using Raven.Client.Documents;
using Raven.Client.Documents.Queries.Facets;

namespace Area52.Services.Implementation.Raven.Statistics;

public class FastStatisticsServices : IFastStatisticsServices
{
    private readonly IDocumentStore documentStore;
    private readonly ILogger<FastStatisticsServices> logger;

    public FastStatisticsServices(IDocumentStore documentStore, ILogger<FastStatisticsServices> logger)
    {
        this.documentStore = documentStore;
        this.logger = logger;
    }

    public async Task<BaseStatistics> GetBaseStatistics(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetBaseStatistics.");

        try
        {
            using var session = this.documentStore.OpenAsyncSession();

            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            DateTimeOffset lastHour = utcNow.AddHours(-1.0);
            DateTimeOffset lastDay = utcNow.AddHours(-24.0);
            int errorLogLevel = (int)LogLevel.Error;
            int criticalLogLevel = (int)LogLevel.Critical;

            Lazy<Task<int>> lazyLogCount = session.Query<LogEntity>()
                .CountLazilyAsync(cancellationToken);
            Lazy<Task<int>> lazyLogLastHourCount = session.Query<LogMainIndex.Result, LogMainIndex>()
                 .Where(t => t.Timestamp >= lastHour)
                 .CountLazilyAsync(cancellationToken);
            Lazy<Task<int>> lazyTsCount = session.Query<Contracts.TimeSeries.TimeSerieDefinition>()
                .CountLazilyAsync(cancellationToken);

            Lazy<Task<int>> lazyErrorLogLastHourCount = session.Query<LogMainIndex.Result, LogMainIndex>()
                 .Where(t => t.Timestamp >= lastHour && t.LevelNumeric == errorLogLevel)
                 .CountLazilyAsync(cancellationToken);

            Lazy<Task<int>> lazyCriticalLogLastDayCount = session.Query<LogMainIndex.Result, LogMainIndex>()
                .Where(t => t.Timestamp >= lastDay && t.LevelNumeric == criticalLogLevel)
                .CountLazilyAsync(cancellationToken);

            DateTime executionStart = DateTime.UtcNow;
            await session.Advanced.Eagerly.ExecuteAllPendingLazyOperationsAsync(cancellationToken);
            TimeSpan executionTime = DateTime.UtcNow - executionStart;

            this.logger.LogDebug("Get statistics from {logsCount} logs in {duration}.",
                await lazyLogCount.Value,
                executionTime);

            return new BaseStatistics()
            {
                TotalLogCount = await lazyLogCount.Value,
                NewLogsPerLastHour = await lazyLogLastHourCount.Value,
                TimeSeriesCount = await lazyTsCount.Value,
                BackendType = BackendType.RavenDb,
                ResponseTime = executionTime,
                ErrorsInLastHour = await lazyErrorLogLastHourCount.Value,
                CriticalInLastDay = await lazyCriticalLogLastDayCount.Value
            };
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error during executionTime GetBaseStatistics.");
            throw;
        }
    }

    public async Task<IReadOnlyList<LogShare>> GetLevelsDistribution(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetLevelsDistribution.");

        try
        {
            using var session = this.documentStore.OpenAsyncSession();
            Dictionary<string, FacetResult> logs = await session.Query<LogMainIndex.Result, LogMainIndex>()
                .AggregateBy(new Facet<LogMainIndex.Result>()
                {
                    DisplayFieldName = "LogLevel",
                    FieldName = t => t.LevelNumeric
                })
                .ExecuteAsync(cancellationToken);


            FacetResult result = logs["LogLevel"];

            LogLevelCalculator calculator = new LogLevelCalculator();
            foreach (FacetValue facet in result.Values)
            {
                calculator.SetNumericResult(facet.Range, facet.Count);
            }

            return calculator.ToList();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error during executionTime GetLevelsDistribution.");
            throw;
        }
    }

    public async Task<IReadOnlyList<ApplicationShare>> GetApplicationsDistribution(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetApplicationsDistribution.");

        const int GetMaxApplications = 8;

        try
        {
            using var session = this.documentStore.OpenAsyncSession();
            Dictionary<string, FacetResult> logs = await session.Query<LogMainIndex.Result, LogMainIndex>()
                .AggregateBy(new Facet()
                {
                    DisplayFieldName = "Applications",
                    FieldName = "Application"
                })
                .ExecuteAsync(cancellationToken);

            FacetResult result = logs["Applications"];
            List<FacetValue> sorted = result.Values;
            sorted.Sort((a, b) => b.Count.CompareTo(a.Count));

            decimal totalCount = (decimal)await session.Query<LogMainIndex.Result, LogMainIndex>()
                .LongCountAsync(cancellationToken);

            List<ApplicationShare> listResult = new List<ApplicationShare>(GetMaxApplications);
            long nonOtherCount = 0;
            System.Globalization.TextInfo textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
            foreach (FacetValue value in sorted.Take(GetMaxApplications - 1))
            {
                nonOtherCount += value.Count;
                listResult.Add(new ApplicationShare(textInfo.ToTitleCase(value.Range), (100.0m * value.Count) / totalCount));
            }

            decimal otherCount = totalCount - nonOtherCount;
            if (otherCount > 0.0m)
            {
                listResult.Add(new ApplicationShare("Other...", (100.0m * otherCount) / totalCount));
            }

            return listResult;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error during executionTime GetApplicationsDistribution.");
            throw;
        }
    }
}
