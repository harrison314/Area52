using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.RavenDbTests.TestUtils;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Raven;
using Raven.TestDriver;

namespace Area52.RavenDbTests;

public class LogManagerTests : RavenTestDriver
{
    [Fact]
    public async Task LogManager_RemoveOldLogs_Success()
    {
        using var store = this.GetDocumentStore();

        await CommonDocuments.PrepareDocuments(store);
        this.WaitForIndexing(store);

        LogManager manager = new LogManager(store, LoggerCreator.Create<LogManager>());

        await manager.RemoveOldLogs(new DateTimeOffset(2012, 8, 8, 0, 0, 0, TimeSpan.Zero), default);

        using (var session = store.OpenSession())
        {
            int count = session.Query<LogEntity>().Count();
            Assert.Equal(2, count);
        }
    }
}
