using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.RavenDbTests.TestUtils;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Raven;
using Raven.Client.Documents.Indexes;
using Raven.TestDriver;

namespace Area52.RavenDbTests;

public class LogWriterTests : RavenTestDriver
{
    [Fact]
    public async Task LogWriter_Write_Success()
    {
        using var store = this.GetDocumentStore();

        await IndexCreation.CreateIndexesAsync(typeof(Program).Assembly,
               store,
               default);

        LogWriter logWriter = new LogWriter(store, LoggerCreator.Create<LogWriter>());

        List<LogEntity> entities = new List<LogEntity>()
        {
            new LogEntity()
            {
                EventId = "1",
                Exception = null,
                Level = "Debug",
                LevelNumeric = 1,
                Message = "Hello log",
                MessageTemplate = "Hello log",
                Properties = new LogEntityProperty[]
                {
                    new LogEntityProperty("Application", "Area52"),
                    new LogEntityProperty("Count", 45.0)
                },
                Timestamp = new DateTimeOffset(2012,8,7, 14,12,45, TimeSpan.Zero)
            }
        };
        await logWriter.Write(entities);

        this.WaitForIndexing(store);
    }
}
