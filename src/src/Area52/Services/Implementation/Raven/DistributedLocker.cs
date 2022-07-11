using Area52.Services.Contracts;
using Area52.Services.Implementation.Raven.Models;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.CompareExchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Implementation.Raven;

public class DistributedLocker : IDistributedLocker
{
    private readonly IDocumentStore documentStore;
    private readonly ILogger<DistributedLocker> logger;

    public DistributedLocker(IDocumentStore documentStore, ILogger<DistributedLocker> logger)
    {
        this.documentStore = documentStore;
        this.logger = logger;
    }

    public async Task<IDistributedLock> TryAquire(string name, TimeSpan resrvedTime, CancellationToken cancellationToken = default)
    {
        this.logger.LogTrace("Entring to TryAquire with name {name} resrvedTime {resrvedTime}.", name, resrvedTime);

        InfLock lockObject = new InfLock($"InfLock/{name}")
        {
            ExpireAt = DateTime.UtcNow + resrvedTime
        };

        PutCompareExchangeValueOperation<InfLock> putCommand = new PutCompareExchangeValueOperation<InfLock>(lockObject.Id,
            lockObject,
            0L,
            null);
        CompareExchangeResult<InfLock> result = await this.documentStore.Operations.SendAsync(putCommand, null, cancellationToken);

        if (result.Successful)
        {
            this.logger.LogDebug("Aquird lock for {name}.", name);
            return new SuccessDistributedLock(result.Index, lockObject.Id, this.documentStore);
        }

        if (result.Value.ExpireAt < DateTime.UtcNow)
        {
            PutCompareExchangeValueOperation<InfLock> putRefreshCommand = new PutCompareExchangeValueOperation<InfLock>(lockObject.Id,
                lockObject,
                result.Index,
                null);
            CompareExchangeResult<InfLock> refreshResult = await this.documentStore.Operations.SendAsync(putRefreshCommand, null, cancellationToken);

            if (refreshResult.Successful)
            {
                this.logger.LogDebug("Aquird lock for {name}.", name);
                return new SuccessDistributedLock(refreshResult.Index, lockObject.Id, this.documentStore);
            }
        }

        this.logger.LogDebug("Lock for name {name} can not aquired.", name);
        return new FailedDistributedLock();
    }
}
