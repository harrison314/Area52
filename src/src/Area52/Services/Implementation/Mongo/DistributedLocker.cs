using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Mongo.Models;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo;

public class MongoDbDistributedLocker : IDistributedLocker
{
    private readonly IMongoDatabase mongoDatabase;
    private readonly IMongoCollection<LockAcquire> locks;
    private readonly FailedDistributedLock failed;


    public MongoDbDistributedLocker(IMongoDatabase mongoDatabase)
    {
        this.mongoDatabase = mongoDatabase;
        this.locks = mongoDatabase.GetCollection<LockAcquire>("LockAcquires");
        this.failed = new FailedDistributedLock();
    }

    public async Task<IDistributedLock> TryAquire(string name, TimeSpan resrvedTime, CancellationToken cancellationToken = default)
    {
        Guid acquireId = Guid.NewGuid();
        if(await this.TryUpdate(resrvedTime, name, acquireId))
        {
            return new SucccessDistributedLock(this.locks, acquireId);
        }
        else
        {
            return this.failed;
        }
    }

    private async Task<bool> TryUpdate(TimeSpan lifetime, string id, Guid acquireId)
    {
        try
        {
            UpdateDefinition<LockAcquire> update = new UpdateDefinitionBuilder<LockAcquire>()
                    .Set(x => x.Acquired, true)
                    .Set(x => x.ExpiresIn, DateTime.UtcNow + lifetime)
                    .Set(x => x.AcquireId, acquireId)
                    .SetOnInsert(x => x.Id, id);

            FilterDefinitionBuilder<LockAcquire> builder = new FilterDefinitionBuilder<LockAcquire>();
            FilterDefinition<LockAcquire> filter = builder.And(
                        builder.Eq(x => x.Id, id),
                        builder.Or(
                            builder.Eq(x => x.Acquired, false),
                            builder.Lte(x => x.ExpiresIn, DateTime.UtcNow)
                        )
                    );

            UpdateResult updateResult = await this.locks.UpdateOneAsync(
                filter: filter, // x => x.Id == _id && (!x.Acquired || x.ExpiresIn <= DateTime.UtcNow),
                update: update, options: new UpdateOptions { IsUpsert = true });

            return updateResult.IsAcknowledged;
        }
        catch (MongoWriteException ex) // E11000 
        {
            if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                return false;

            throw;
        }
    }
}
