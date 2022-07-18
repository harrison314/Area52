using Area52.Services.Contracts;
using Area52.Services.Implementation.Raven.Indexes;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Implementation.Raven;

public class LogManager : ILogManager
{
    private readonly IDocumentStore documentStore;
    private readonly ILogger<LogManager> logger;

    public LogManager(IDocumentStore documentStore, ILogger<LogManager> logger)
    {
        this.documentStore = documentStore;
        this.logger = logger;
    }

    public async Task RemoveOldLogs(DateTimeOffset timeAtDeteledLogs, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to RemoveOldLogs with timeAtDeteledLogs={timeAtDeteledLogs}", timeAtDeteledLogs);

        DeleteByQueryOperation<LogMainIndex.Result, LogMainIndex> operation = new DeleteByQueryOperation<LogMainIndex.Result, LogMainIndex>(
         t => t.Timestamp < timeAtDeteledLogs);

        this.logger.LogDebug("Staring removing logs");
        Operation operationResult = await this.documentStore.Operations.SendAsync(operation, token: cancellationToken);
        IOperationResult result = await operationResult.WaitForCompletionAsync();

        this.logger.LogInformation("Removed old logs to {timeAtDeteledLogs}.", timeAtDeteledLogs);
    }
}
