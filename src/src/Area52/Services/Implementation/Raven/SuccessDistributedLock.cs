using Area52.Services.Contracts;
using Area52.Services.Implementation.Raven.Models;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.CompareExchange;

namespace Area52.Services.Implementation.Raven;

internal class SuccessDistributedLock : IDistributedLock
{
    private readonly long index;
    private readonly string id;
    private readonly IDocumentStore documentStore;

    public bool Aquired
    {
        get => true;
    }

    public SuccessDistributedLock(long index, string id, IDocumentStore documentStore)
    {
        this.index = index;
        this.id = id;
        this.documentStore = documentStore;
    }

    public async ValueTask DisposeAsync()
    {
        await this.documentStore.Operations.SendAsync(
            new DeleteCompareExchangeValueOperation<InfLock>(
                this.id,
                this.index)
            );
    }
}
