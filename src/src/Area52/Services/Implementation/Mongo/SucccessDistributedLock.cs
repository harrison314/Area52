using Area52.Services.Contracts;
using Area52.Services.Implementation.Mongo.Models;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo;

internal class SuccessDistributedLock : IDistributedLock
{
    private readonly IMongoCollection<LockAcquire> locks;
    private readonly Guid acquiredId;

    public bool Acquired
    {
        get => true;
    }

    public SuccessDistributedLock(IMongoCollection<LockAcquire> locks, Guid acquiredId)
    {
        this.locks = locks;
        this.acquiredId = acquiredId;
    }

    public async ValueTask DisposeAsync()
    {
        await this.locks.DeleteOneAsync(t => t.AcquireId == this.acquiredId);
    }
}
