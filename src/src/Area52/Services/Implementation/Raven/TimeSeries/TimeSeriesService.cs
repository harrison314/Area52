using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts.TimeSeries;
using Area52.Services.Implementation.Raven.Indexes;
using Raven.Client.Documents;
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
            this.logger.LogError(ex, "Unexcepted error in GetDefinitionForExecute with executeBefore {executeBefore}.", executeBefore);
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
            this.logger.LogError(ex, "Unexcepted error in GetDefinitionUnit method with id {id}.", id);
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
            this.logger.LogError(ex, "Unexcepted error in CreateWriter method with definitionId {definitionId}.", definitionId);
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

            this.logger.LogInformation("Confirm writing timeserie data into definition with id {definitionId}.", definitionId);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexcepted error in ConfirmWriting method with definitionId {definitionId}.", definitionId);
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

            AgregateFunctionRuntime agregateRuntime = AgregateFunctionHelper.GetFunctions(request.AgregationFunction);

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

            return agregateRuntime.Mapper(ts);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexcepted error in ExecuteQuery method with DefinitionId {DefinitionId}.", request.DefinitionId);
            throw;
        }
        finally
        {
            this.logger.LogDebug("Exiting to ExecuteQuery with DefinitionId {DefinitionId}.", request.DefinitionId);
        }
    }
}
