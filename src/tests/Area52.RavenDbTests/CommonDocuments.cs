using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.RavenDbTests.TestUtils;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Raven;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.TestDriver;

namespace Area52.RavenDbTests;

internal static class CommonDocuments
{
    public static async Task PrepareDocuments(IDocumentStore store)
    {
        await IndexCreation.CreateIndexesAsync(typeof(Program).Assembly,
               store,
               default);

        LogWriter logWriter = new LogWriter(store, LoggerCreator.Create<LogWriter>());

        List<LogEntity> entities = new List<LogEntity>()
        {
            new LogEntity()
            {
                EventId = 1,
                Exception = null,
                Level = "Debug",
                LevelNumeric = 1,
                Message = "Hello log",
                MessageTemplate = "Hello log",
                Properties = new LogEntityProperty[]
                {
                    new LogEntityProperty("Application", "Area52"),
                    new LogEntityProperty("Count", 45.0),
                    new LogEntityProperty("Number", 1),
                },
                Timestamp = new DateTimeOffset(2012,8,6, 14,12,45, TimeSpan.Zero)
            },
            new LogEntity()
            {
                EventId = 1,
                Exception = null,
                Level = "Information",
                LevelNumeric = 2,
                Message = "Hello log",
                MessageTemplate = "Hello log",
                Properties = new LogEntityProperty[]
                {
                    new LogEntityProperty("Application", "Area52"),
                    new LogEntityProperty("Count", 45.0),
                    new LogEntityProperty("Number", 2),
                },
                Timestamp = new DateTimeOffset(2012,8,7, 14,12,45, TimeSpan.Zero)
            },
            new LogEntity()
            {
                EventId = 1,
                Exception = null,
                Level = "Warning",
                LevelNumeric = 3,
                Message = "Red bugs.",
                MessageTemplate = "{LogMessage}",
                Properties = new LogEntityProperty[]
                {
                    new LogEntityProperty("Application", "Area52"),
                    new LogEntityProperty("Count", 45.0),
                    new LogEntityProperty("Number", 3),
                    new LogEntityProperty("LogMessage", "Red bugs.")
                },
                Timestamp = new DateTimeOffset(2012,8,8, 14,12,45, TimeSpan.Zero)
            },
            new LogEntity()
            {
                EventId = 1,
                Exception = null,
                Level = "Debug",
                LevelNumeric = 1,
                Message = "Hello log",
                MessageTemplate = "Hello log",
                Properties = new LogEntityProperty[]
                {
                    new LogEntityProperty("Application", "TestApp"),
                    new LogEntityProperty("Count", 45.0),
                    new LogEntityProperty("Number", 4),
                },
                Timestamp = new DateTimeOffset(2012,8,9, 14,12,45, TimeSpan.Zero)
            }
        };
        await logWriter.Write(entities);
    }
}
