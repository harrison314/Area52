using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
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
            this.logger.LogError(ex, "Unexpected error in Create method.");
            throw;
        }
    }

    public async Task Delete(string id)
    {
        this.logger.LogTrace("Entering to Delete with id {id}.", id);

        try
        {
            using var session = this.documentStore.OpenAsyncSession();
            TimeSerieDefinition definition = await session.LoadAsync<TimeSerieDefinition>(id);
            if (definition == null)
            {
                this.logger.LogError("Time series definition with id {id} not found.", id);
                throw new Area52Exception($"Time series definition with id {id} not found.");
            }

            session.Delete(definition);

            await session.SaveChangesAsync();
            this.logger.LogInformation("Removed TimeSeries with id {id}, name {tsName}.", id, definition.Name);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error in Delete method with id {id}.", id);
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
            this.logger.LogError(ex, "Unexpected error in FindById method with id {id}.", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<TimeSerieDefinitionInfo>> FindDefinictions()
    {
        this.logger.LogTrace("Entering to FindDefinictions.");

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
            this.logger.LogError(ex, "Unexpected error in FindDefinictions method.");
            throw;
        }
    }
}
