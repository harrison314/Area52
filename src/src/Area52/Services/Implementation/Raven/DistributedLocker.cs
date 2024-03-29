﻿using Area52.Services.Contracts;
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

    public async Task<IDistributedLock> TryAcquire(string name, TimeSpan reservedTime, CancellationToken cancellationToken = default)
    {
        this.logger.LogTrace("Entering to TryAcquire with name {name} reservedTime {reservedTime}.", name, reservedTime);

        InfLock lockObject = new InfLock($"InfLock/{name}")
        {
            ExpireAt = DateTime.UtcNow + reservedTime
        };

        PutCompareExchangeValueOperation<InfLock> putCommand = new PutCompareExchangeValueOperation<InfLock>(lockObject.Id,
            lockObject,
            0L,
            null);
        CompareExchangeResult<InfLock> result = await this.documentStore.Operations.SendAsync(putCommand, null, cancellationToken);

        if (result.Successful)
        {
            this.logger.LogDebug("Acquired lock for {name}.", name);
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
                this.logger.LogDebug("Acquired lock for {name}.", name);
                return new SuccessDistributedLock(refreshResult.Index, lockObject.Id, this.documentStore);
            }
        }

        this.logger.LogDebug("Lock for name {name} can not acquired.", name);
        return new FailedDistributedLock();
    }
}
