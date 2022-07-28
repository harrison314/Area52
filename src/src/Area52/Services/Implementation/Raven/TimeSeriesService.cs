using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Raven.Indexes;
using Area52.Services.Implementation.Raven.Models;
using Raven.Client.Documents;

namespace Area52.Services.Implementation.Raven;

public class TimeSeriesService : ITimeSeriesService
{
    internal const string TsName = "DefaultTs";
    private readonly IDocumentStore documentStore;
    private readonly ILogger<RavenDbIndexJob> logger;

    public TimeSeriesService(IDocumentStore documentStore, ILogger<RavenDbIndexJob> logger)
    {
        this.documentStore = documentStore;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<TimeSeriesDefinitionId>> GetDefinitionForExecute(DateTime executeBefore, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetDefinitionForExecute with executeBefore={executeBefore}.", executeBefore);

        using global::Raven.Client.Documents.Session.IAsyncDocumentSession session = this.documentStore.OpenAsyncSession();

        List<string> ids = await session.Query<TimeSerieDefinitionIndex.Result, TimeSerieDefinitionIndex>().Where(t => t.LastExecute == null || t.LastExecute < executeBefore)
             .Select(t => t.Id)
             .ToListAsync(cancellationToken);

        List<TimeSeriesDefinitionId> result = new List<TimeSeriesDefinitionId>(ids.Count);
        result.AddRange(ids.Select(t => new TimeSeriesDefinitionId(t)));

        return result;
    }

    public async Task<TimeSeriesDefinitionUnit> GetDefinitionUnit(string id, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetDefinitionUnit with id={id}.", id);

        using global::Raven.Client.Documents.Session.IAsyncDocumentSession session = this.documentStore.OpenAsyncSession();

        TimeSerieDefinition definition = await session.LoadAsync<TimeSerieDefinition>(id, cancellationToken);
        return new TimeSeriesDefinitionUnit(definition.Id,
            definition.Name,
            definition.Query,
            definition.ValueFieldName,
            definition.TagFieldName,
            definition?.LastExecutionInfo?.LastExecute);
    }

    public async Task<ITimeSeriesWriter> CreateWriter(string definitionId, string definitionName, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to CreateWriter with definitionId={definitionId}, definitionName={definitionName}.", definitionId, definitionName);

        using global::Raven.Client.Documents.Session.IAsyncDocumentSession session = this.documentStore.OpenAsyncSession();
        TimeSerieContainer? container = await session.Query<TimeSerieContainerIndex.Result, TimeSerieContainerIndex>()
            .Where(t => t.TimeSerieDefinitionId == definitionId)
            .OfType<TimeSerieContainer>()
            .FirstOrDefaultAsync(cancellationToken);

        if (container == null)
        {
            container = new TimeSerieContainer()
            {
                TimeSerieDefinitionId = definitionId,
                Name = definitionName
            };

            await session.StoreAsync(container, cancellationToken);
            session.TimeSeriesFor(container, TsName);
            await session.SaveChangesAsync(cancellationToken);
        }

        string containerId = session.Advanced.GetDocumentId(container);
        return new TimeSeriesWriter(this.documentStore, containerId);
    }

    public async Task ConfirmWriting(string definitionId, LastExecutionInfo lastExecutionInfo, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to ConfirmWriting with definitionId={definitionId}.", definitionId);

        using global::Raven.Client.Documents.Session.IAsyncDocumentSession session = this.documentStore.OpenAsyncSession();
        TimeSerieDefinition tsd = await session.LoadAsync<TimeSerieDefinition>(definitionId, cancellationToken);
        tsd.LastExecutionInfo = lastExecutionInfo;
        await session.SaveChangesAsync(cancellationToken);
    }
}

internal class TimeSeriesWriter : ITimeSeriesWriter
{
    private record struct TsEntity(DateTimeOffset timestamp, double value, string? tag);
    private readonly List<TsEntity> buffer;
    private readonly IDocumentStore documentStore;
    private readonly string containerId;

    public TimeSeriesWriter(IDocumentStore documentStore, string containerId)
    {
        this.buffer = new List<TsEntity>(100);
        this.documentStore = documentStore;
        this.containerId = containerId;
    }
    public ValueTask Write(DateTimeOffset timestamp, double value, string? tag)
    {
        this.buffer.Add(new TsEntity(timestamp, value, tag));

        if (this.buffer.Count > 99)
        {
            return this.FlushBuffer();
        }

        return new ValueTask();
    }

    public async ValueTask DisposeAsync()
    {
        if (this.buffer.Count > 0)
        {
            await this.FlushBuffer();
        }
    }

    private async ValueTask FlushBuffer()
    {
        using global::Raven.Client.Documents.Session.IAsyncDocumentSession session = this.documentStore.OpenAsyncSession();

        global::Raven.Client.Documents.Session.IAsyncSessionDocumentTimeSeries ts = session.TimeSeriesFor(this.containerId, TimeSeriesService.TsName);

        foreach (TsEntity entity in this.buffer)
        {
            ts.Append(entity.timestamp.UtcDateTime, new double[] { entity.value }, entity.tag);
        }

        await session.SaveChangesAsync();
        this.buffer.Clear();
    }
}
