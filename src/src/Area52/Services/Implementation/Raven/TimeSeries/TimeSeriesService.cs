using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Raven.Indexes;
using Area52.Services.Implementation.Raven.Models;
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

        using var session = this.documentStore.OpenAsyncSession();

        var ids = await session.Query<TimeSerieDefinitionIndex.Result, TimeSerieDefinitionIndex>().Where(t => t.LastExecute == null || t.LastExecute < executeBefore)
             .Select(t => t.Id)
             .ToListAsync(cancellationToken);

        var result = new List<TimeSeriesDefinitionId>(ids.Count);
        result.AddRange(ids.Select(t => new TimeSeriesDefinitionId(t)));

        return result;
    }

    public async Task<TimeSeriesDefinitionUnit> GetDefinitionUnit(string id, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetDefinitionUnit with id={id}.", id);

        using var session = this.documentStore.OpenAsyncSession();

        TimeSerieDefinition definition = await session.LoadAsync<TimeSerieDefinition>(id, cancellationToken);
        return new TimeSeriesDefinitionUnit(definition.Id,
            definition.Name,
            definition.Query,
            definition.ValueFieldName,
            definition.TagFieldName,
            definition?.LastExecutionInfo?.LastExecute);
    }

    public Task<ITimeSeriesWriter> CreateWriter(string definitionId, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to CreateWriter with definitionId={definitionId}.", definitionId);

        return Task.FromResult<ITimeSeriesWriter>(new TimeSeriesWriter(this.documentStore,
            definitionId,
            this.loggerFactory.CreateLogger<TimeSeriesWriter>()));
    }

    public async Task ConfirmWriting(string definitionId, LastExecutionInfo lastExecutionInfo, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to ConfirmWriting with definitionId={definitionId}.", definitionId);

        using var session = this.documentStore.OpenAsyncSession();
        var tsd = await session.LoadAsync<TimeSerieDefinition>(definitionId, cancellationToken);
        tsd.LastExecutionInfo = lastExecutionInfo;
        await session.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Confirm writing timeserie data into definition with id {definitionId}.", definitionId);
    }

    public async Task<IReadOnlyList<TimeSeriesItem>> ExecuteQuery(TimeSeriesQueryRequest request, CancellationToken cancellationToken)
    {

        Action<ITimePeriodBuilder> groupingAction = request.GroupByFunction switch //input is a string that represents some client input
        {
            TimeSeriesGroupByFn.Seconds => (Action<ITimePeriodBuilder>)(builder => builder.Seconds(1)),
            TimeSeriesGroupByFn.Minutes => (Action<ITimePeriodBuilder>)(builder => builder.Minutes(1)),
            TimeSeriesGroupByFn.Hours => (Action<ITimePeriodBuilder>)(builder => builder.Hours(1)),
            TimeSeriesGroupByFn.Days => (Action<ITimePeriodBuilder>)(builder => builder.Days(1)),
            TimeSeriesGroupByFn.Months => (Action<ITimePeriodBuilder>)(builder => builder.Months(1)),
            TimeSeriesGroupByFn.Quarters => (Action<ITimePeriodBuilder>)(builder => builder.Quarters(1)),
            TimeSeriesGroupByFn.Years => (Action<ITimePeriodBuilder>)(builder => builder.Years(1)),
            _ => throw new InvalidProgramException($"Enum value {request.GroupByFunction} is not supported.")
        };

        using var session = this.documentStore.OpenAsyncSession();
        var ts = await session.Query<TimeSerieDefinition>()
            .Where(t => t.Id == request.DefinitionId)
            .Select(t => global::Raven.Client.Documents.Queries.RavenQuery.TimeSeries(t, TimeSeriesConstants.TsName, request.From, request.To)
                .GroupBy(groupingAction)
                 .Select(t => t.Sum())
                 .ToList()
                 )
            .SingleAsync(cancellationToken);

        List<TimeSeriesItem> result = new List<TimeSeriesItem>(ts.Results.Length);
        for (int i = 0; i < ts.Results.Length; i++)
        {
            result.Add(new TimeSeriesItem()
            {
                From = ts.Results[i].From,
                Value = ts.Results[i].Sum[0]
            });
        }

        return result;
    }
}
