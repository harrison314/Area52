using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Raven.Client.Documents;

namespace Area52.Services.Implementation.Raven.TimeSeries;

public class TimeSerieDefinitionsRepository : ITimeSerieDefinitionsRepository
{
    private readonly IDocumentStore documentStore;
    private readonly ILogger<TimeSerieDefinitionsRepository> logger;

    public TimeSerieDefinitionsRepository(IDocumentStore documentStore, ILogger<TimeSerieDefinitionsRepository> logger)
    {
        this.documentStore = documentStore;
        this.logger = logger;
    }

    public async Task<string> Create(TimeSerieDefinition timeSerieDefinition)
    {
        this.logger.LogTrace("Entering to Create.");

        using var session = this.documentStore.OpenAsyncSession();

        await session.StoreAsync(timeSerieDefinition);
        await session.SaveChangesAsync();

        return timeSerieDefinition.Id;
    }

    public async Task<TimeSerieDefinition> FindById(string id)
    {
        this.logger.LogTrace("Entering to FindById with id {id}.", id);

        using var session = this.documentStore.OpenAsyncSession();
        return await session.LoadAsync<TimeSerieDefinition>(id);
    }

    public async Task<IReadOnlyList<TimeSerieDefinitionInfo>> FindDefinictions()
    {
        this.logger.LogTrace("Entering to Create.");

        using var session = this.documentStore.OpenAsyncSession();
        return await session.Query<TimeSerieDefinition>().Select(t => new TimeSerieDefinitionInfo()
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description
        }).ToListAsync();
    }
}
