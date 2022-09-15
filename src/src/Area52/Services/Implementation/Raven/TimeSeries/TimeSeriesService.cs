using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts.TimeSeries;
using Area52.Services.Implementation.Raven.Indexes;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.TimeSeries;
using Raven.Client.Documents.Queries.TimeSeries;

namespace Area52.Services.Implementation.Raven.TimeSeries;

public class TimeSeriesService : ITimeSeriesService
{
    private readonly IDocumentStore documentStore;
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger<TimeSeriesService> logger;

    public TimeSeriesService(IDocumentStore documentStore, ILoggerFactory loggerFactory)
    {
        this.documentStore = documentStore;
        this.loggerFactory = loggerFactory;
        this.logger = loggerFactory.CreateLogger<TimeSeriesService>();
    }

    public async Task<IReadOnlyList<TimeSeriesDefinitionId>> GetDefinitionForExecute(DateTime executeBefore, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetDefinitionForExecute with executeBefore={executeBefore}.", executeBefore);

        try
        {
            using var session = this.documentStore.OpenAsyncSession();

            List<string> ids = await session.Query<TimeSerieDefinitionIndex.Result, TimeSerieDefinitionIndex>().Where(t => t.LastExecute == null || t.LastExecute < executeBefore)
                 .Select(t => t.Id)
                 .ToListAsync(cancellationToken);

            var result = new List<TimeSeriesDefinitionId>(ids.Count);
            result.AddRange(ids.Select(t => new TimeSeriesDefinitionId(t)));

            return result;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in GetDefinitionForExecute with executeBefore {executeBefore}.", executeBefore);
            throw;
        }
    }

    public async Task<TimeSeriesDefinitionUnit> GetDefinitionUnit(string id, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetDefinitionUnit with id={id}.", id);

        try
        {
            using var session = this.documentStore.OpenAsyncSession();

            TimeSerieDefinition definition = await session.LoadAsync<TimeSerieDefinition>(id, cancellationToken);
            return new TimeSeriesDefinitionUnit(definition.Id,
                definition.Name,
                definition.Query,
                definition.ValueFieldName,
                definition.TagFieldName,
                definition?.LastExecutionInfo?.LastExecute);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in GetDefinitionUnit method with id {id}.", id);
            throw;
        }
    }

    public Task<ITimeSeriesWriter> CreateWriter(string definitionId, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to CreateWriter with definitionId={definitionId}.", definitionId);

        try
        {
            return Task.FromResult<ITimeSeriesWriter>(new TimeSeriesWriter(this.documentStore,
                definitionId,
                this.loggerFactory.CreateLogger<TimeSeriesWriter>()));
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in CreateWriter method with definitionId {definitionId}.", definitionId);
            throw;
        }
    }

    public async Task ConfirmWriting(string definitionId, LastExecutionInfo lastExecutionInfo, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to ConfirmWriting with definitionId={definitionId}.", definitionId);

        try
        {
            using var session = this.documentStore.OpenAsyncSession();
            var tsd = await session.LoadAsync<TimeSerieDefinition>(definitionId, cancellationToken);
            tsd.LastExecutionInfo = lastExecutionInfo;
            await session.SaveChangesAsync(cancellationToken);

            this.logger.LogInformation("Confirm writing time series data into definition with id {definitionId}.", definitionId);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in ConfirmWriting method with definitionId {definitionId}.", definitionId);
            throw;
        }
    }

    public async Task<IReadOnlyList<TimeSeriesItem>> ExecuteQuery(TimeSeriesQueryRequest request, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to ExecuteQuery with DefinitionId {DefinitionId}.", request.DefinitionId);

        try
        {
            Action<ITimePeriodBuilder> groupingAction = request.GroupByFunction switch //input is a string that represents some client input
            {
                TimeSeriesGroupByFn.Seconds => (Action<ITimePeriodBuilder>)(builder => builder.Seconds(1)),
                TimeSeriesGroupByFn.Minutes => (Action<ITimePeriodBuilder>)(builder => builder.Minutes(1)),
                TimeSeriesGroupByFn.Hours => (Action<ITimePeriodBuilder>)(builder => builder.Hours(1)),
                TimeSeriesGroupByFn.Days => (Action<ITimePeriodBuilder>)(builder => builder.Days(1)),
                TimeSeriesGroupByFn.Weeks => (Action<ITimePeriodBuilder>)(builder => builder.Days(7)),
                TimeSeriesGroupByFn.Months => (Action<ITimePeriodBuilder>)(builder => builder.Months(1)),
                TimeSeriesGroupByFn.Quarters => (Action<ITimePeriodBuilder>)(builder => builder.Quarters(1)),
                TimeSeriesGroupByFn.Years => (Action<ITimePeriodBuilder>)(builder => builder.Years(1)),
                _ => throw new InvalidProgramException($"Enum value {request.GroupByFunction} is not supported.")
            };

            AggregateFunctionRuntime aggregateRuntime = AggregateFunctionHelper.GetFunctions(request.AggregationFunction);

            using var session = this.documentStore.OpenAsyncSession();

            TimeSeriesAggregationResult ts = await session.Query<TimeSerieDefinition>()
                .Where(t => t.Id == request.DefinitionId)
                .Select(t => global::Raven.Client.Documents.Queries.RavenQuery.TimeSeries(t, TimeSeriesConstants.TsName, request.From, request.To)
                    .GroupBy(groupingAction)
                     .Select(t => new
                     {
                         // TODO: fix selection
                         Count = t.Count(),
                         Sum = t.Sum(),
                         Min = t.Min(),
                         Max = t.Max(),
                         Average = t.Average()
                     })
                     .ToList()
                     )
                .SingleAsync(cancellationToken);

            return aggregateRuntime.Mapper(ts);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in ExecuteQuery method with DefinitionId {DefinitionId}.", request.DefinitionId);
            throw;
        }
        finally
        {
            this.logger.LogDebug("Exiting to ExecuteQuery with DefinitionId {DefinitionId}.", request.DefinitionId);
        }
    }

    public async Task DeleteOldData(DateTime olderDate, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to DeleteOldData with olderDate {olderDate}.", olderDate);

        try
        {
            using var session = this.documentStore.OpenAsyncSession();
            List<string> tsd = await session.Query<TimeSerieDefinition>()
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            foreach (string timeSerieDefinitionId in tsd)
            {
                this.logger.LogTrace("Sending delete command to time series definition with id {timeSerieDefinitionId}.", timeSerieDefinitionId);
                TimeSeriesOperation removeEntries = new TimeSeriesOperation()
                {
                    Name = TimeSeriesConstants.TsName
                };

                removeEntries.Delete(new TimeSeriesOperation.DeleteOperation()
                {
                    From = null,
                    To = olderDate
                });

                TimeSeriesBatchOperation removeEntriesBatch = new TimeSeriesBatchOperation(timeSerieDefinitionId, removeEntries);
                await this.documentStore.Operations.SendAsync(removeEntriesBatch, token: cancellationToken);

                this.logger.LogDebug("Send delete command to time series definition with id {timeSerieDefinitionId} older to {olderDate}.", timeSerieDefinitionId, olderDate);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in DeleteOldData method with olderDate {olderDate}.", olderDate);
            throw;
        }
    }
}
