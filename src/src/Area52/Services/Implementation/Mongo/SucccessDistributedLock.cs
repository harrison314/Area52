using Area52.Services.Contracts;
using Area52.Services.Implementation.Mongo.Models;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo;

internal class SucccessDistributedLock : IDistributedLock
{
    private readonly IMongoCollection<LockAcquire> locks;
    private readonly Guid aquiredId;

    public bool Acquired
    {
        get => true;
    }

    public SucccessDistributedLock(IMongoCollection<LockAcquire> locks, Guid aquiredId)
    {
        this.locks = locks;
        this.aquiredId = aquiredId;
    }

    public async ValueTask DisposeAsync()
    {
        await this.locks.DeleteOneAsync(t => t.AcquireId == this.aquiredId);
    }
}
