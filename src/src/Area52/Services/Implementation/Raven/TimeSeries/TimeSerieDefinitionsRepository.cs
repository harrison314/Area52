using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts.TimeSeries;
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

        try
        {
            using var session = this.documentStore.OpenAsyncSession();

            await session.StoreAsync(timeSerieDefinition);
            await session.SaveChangesAsync();

            return timeSerieDefinition.Id;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexcepted error in Create method.");
            throw;
        }
    }

    public async Task<TimeSerieDefinition> FindById(string id)
    {
        this.logger.LogTrace("Entering to FindById with id {id}.", id);

        try
        {
            using var session = this.documentStore.OpenAsyncSession();
            return await session.LoadAsync<TimeSerieDefinition>(id);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexcepted error in FindById method with id {id}.", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<TimeSerieDefinitionInfo>> FindDefinictions()
    {
        this.logger.LogTrace("Entering to Create.");

        try
        {
            using var session = this.documentStore.OpenAsyncSession();
            return await session.Query<TimeSerieDefinition>().Select(t => new TimeSerieDefinitionInfo()
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description
            }).ToListAsync();

        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexcepted error in FindDefinictions method.");
            throw;
        }
    }
}
