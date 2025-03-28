using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.RavenDbTests.TestUtils;
using Area52.Services.Configuration;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Raven;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.TestDriver;

namespace Area52.RavenDbTests;

public class LogReaderTests : RavenTestDriver
{
    [Fact]
    public async Task LogReader_LoadLogInfo_ReturnsNull()
    {
        using var store = this.GetDocumentStore();

        await CommonDocuments.PrepareDocuments(store);
        this.WaitForIndexing(store);

        LogReader reader = new LogReader(store, this.CreateOptions(), LoggerCreator.Create<LogReader>());

        LogEntity? result = await reader.LoadLogInfo("LogEntitys/4589-F");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("Application is 'TestApp'", 1)]
    [InlineData("Application is 'TestApp' or Number == 2", 2)]
    [InlineData("Number <= 3", 3)]
    [InlineData("Application startsWith 'Area'", 3)]
    [InlineData("Application is 'Area52'", 3)]
    [InlineData("Application startsWith 'Area' and Number is 589", 0)]
    public async Task LogReader_ReadLastLogs_Returns(string query, int exceptedCount)
    {
        using var store = this.GetDocumentStore();

        await CommonDocuments.PrepareDocuments(store);
        this.WaitForIndexing(store);

        LogReader reader = new LogReader(store, this.CreateOptions(), LoggerCreator.Create<LogReader>());

        ReadLastLogResult result = await reader.ReadLastLogs(query);

        Assert.Equal(exceptedCount, result.Logs.Count);
        Assert.Equal(exceptedCount, result.TotalResults);
    }

    [Theory]
    [InlineData("Application is 'TestApp'", 1)]
    [InlineData("Application is 'TestApp' or Number == 2", 2)]
    [InlineData("Number <= 3", 3)]
    [InlineData("Application startsWith 'Area'", 3)]
    [InlineData("Application startsWith 'Area' and Number is 589", 0)]
    public async Task LogReader_ReadLogs_Returns(string query, int exceptedCount)
    {
        using var store = this.GetDocumentStore();

        await CommonDocuments.PrepareDocuments(store);
        this.WaitForIndexing(store);

        LogReader reader = new LogReader(store, this.CreateOptions(), LoggerCreator.Create<LogReader>());

        IAsyncEnumerable<LogEntity> result = reader.ReadLogs(query, 50);
        IReadOnlyList<LogEntity> materializedResult = await this.MaterializeResult(result);

        Assert.Equal(exceptedCount, materializedResult.Count);
    }

    private async Task<IReadOnlyList<LogEntity>> MaterializeResult(IAsyncEnumerable<LogEntity> logs)
    {
        List<LogEntity> result = new List<LogEntity>();
        await foreach (LogEntity entity in logs)
        {
            result.Add(entity);
        }

        return result;
    }

    private IOptions<Area52Setup> CreateOptions()
    {
        return Options.Create(new Area52Setup()
        {
            LogView = new LogViewSetup()
            {
                MaxLogShow = 150
            }
        });
    }
}
