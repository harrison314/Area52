using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.RavenDbTests.TestUtils;
using Area52.Services.Implementation.Raven.TimeSeries;
using Raven.Client.Documents;
using Raven.TestDriver;

namespace Area52.RavenDbTests.TimeSeries;

public class TimeSerieDefinitionsRepositoryTests : RavenTestDriver
{
    [Fact]
    public async Task TimeSerieDefinitionsRepository_Create_Success()
    {
        using IDocumentStore store = this.GetDocumentStore();

        TimeSerieDefinitionsRepository respoitory = new TimeSerieDefinitionsRepository(store, LoggerCreator.Create<TimeSerieDefinitionsRepository>());

        Services.Contracts.TimeSeries.TimeSerieDefinition definition = new Services.Contracts.TimeSeries.TimeSerieDefinition()
        {
            DefaultAggregationFunction = Services.Contracts.TimeSeries.AggregateFn.Sum,
            Description = "Any description",
            Enabled = true,
            Name = "First definition",
            Query = "Application is 'Area52'",
            ShowGraphTime = Services.Contracts.TimeSeries.ShowGraphTime.LastYear,
            TagFieldName = null,
            ValueFieldName = null,
            Metadata = new Services.Contracts.UserObjectMetadata()
            {
                Created = DateTime.Now,
                CreatedBy = "User1",
                CreatedById = "User/1-A"
            }
        };

        await respoitory.Create(definition);
    }

    [Fact]
    public async Task TimeSerieDefinitionsRepository_FindById_Success()
    {
        using IDocumentStore store = this.GetDocumentStore();

        string id = await this.CreateDefinitionInternal(store);
        TimeSerieDefinitionsRepository respoitory = new TimeSerieDefinitionsRepository(store, LoggerCreator.Create<TimeSerieDefinitionsRepository>());

        Services.Contracts.TimeSeries.TimeSerieDefinition result = await respoitory.FindById(id);

        Assert.NotNull(result);
        Assert.NotNull(result.Id);
        Assert.NotNull(result.Name);
        Assert.NotNull(result.Description);
    }


    [Fact]
    public async Task TimeSerieDefinitionsRepository_FindDefinictions()
    {
        using IDocumentStore store = this.GetDocumentStore();

        string id = await this.CreateDefinitionInternal(store);
        TimeSerieDefinitionsRepository respoitory = new TimeSerieDefinitionsRepository(store, LoggerCreator.Create<TimeSerieDefinitionsRepository>());

        IReadOnlyList<Services.Contracts.TimeSeries.TimeSerieDefinitionInfo> result = await respoitory.FindDefinictions();

        Assert.Equal(1, result.Count);
        Assert.NotNull(result[0].Id);
        Assert.NotNull(result[0].Name);
        Assert.NotNull(result[0].Description);
    }

    private async Task<string> CreateDefinitionInternal(IDocumentStore store)
    {
        TimeSerieDefinitionsRepository respoitory = new TimeSerieDefinitionsRepository(store, LoggerCreator.Create<TimeSerieDefinitionsRepository>());

        Services.Contracts.TimeSeries.TimeSerieDefinition definition = new Services.Contracts.TimeSeries.TimeSerieDefinition()
        {
            DefaultAggregationFunction = Services.Contracts.TimeSeries.AggregateFn.Sum,
            Description = "Any description",
            Enabled = true,
            Name = "First definition",
            Query = "Application is 'Area52'",
            ShowGraphTime = Services.Contracts.TimeSeries.ShowGraphTime.LastYear,
            TagFieldName = null,
            ValueFieldName = null,
            Metadata = new Services.Contracts.UserObjectMetadata()
            {
                Created = DateTime.Now,
                CreatedBy = "User1",
                CreatedById = "User/1-A"
            }
        };

        await respoitory.Create(definition);

        this.WaitForIndexing(store);

        using (var session = store.OpenSession())
        {
            return session.Query<Services.Contracts.TimeSeries.TimeSerieDefinition>()
                   .First()
                   .Id;
        }
    }
}
