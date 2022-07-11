using Area52.Services.Contracts;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Implementation.Raven;

internal class LogWriter : ILogWriter
{
    private readonly IDocumentStore documentStore;
    private readonly ILogger<LogWriter> logger;

    public LogWriter(IDocumentStore documentStore, ILogger<LogWriter> logger)
    {
        this.documentStore = documentStore;
        this.logger = logger;
    }

    public async Task Write(IReadOnlyList<LogEntity> logs)
    {
        this.logger.EnteringToWrite();

        using BulkInsertOperation operation = this.documentStore.BulkInsert();
        foreach (LogEntity entity in logs)
        {
            await operation.StoreAsync(entity);
        }

        this.logger.SucessufullWritedLogs(logs.Count);
    }
}
