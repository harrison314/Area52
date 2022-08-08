using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.RavenDbTests.TestUtils;
using Area52.Services.Implementation.Raven;
using Raven.TestDriver;

namespace Area52.RavenDbTests;

public class DistributedLockerTests : RavenTestDriver
{
    [Fact]
    public async Task DistributedLocker_TryAcquire_Lock()
    {
        using var store = this.GetDocumentStore();

        DistributedLocker distributedLocker = new DistributedLocker(store, LoggerCreator.Create<DistributedLocker>());

        await using (Services.Contracts.IDistributedLock locker = await distributedLocker.TryAcquire("textLock", TimeSpan.FromMinutes(1.0)))
        {
            Assert.True(locker.Acquired);
        }

        await using (Services.Contracts.IDistributedLock locker = await distributedLocker.TryAcquire("textLock", TimeSpan.FromMinutes(1.0)))
        {
            Assert.True(locker.Acquired);
        }
    }

    [Fact]
    public async Task DistributedLocker_TryAcquire_NotAquired()
    {
        using var store = this.GetDocumentStore();

        DistributedLocker distributedLocker = new DistributedLocker(store, LoggerCreator.Create<DistributedLocker>());

        await using (Services.Contracts.IDistributedLock locker = await distributedLocker.TryAcquire("textLock", TimeSpan.FromMinutes(1.0)))
        {
            Assert.True(locker.Acquired);
            await using (Services.Contracts.IDistributedLock innecrLocker = await distributedLocker.TryAcquire("textLock", TimeSpan.FromMinutes(1.0)))
            {
                Assert.False(innecrLocker.Acquired);
            }
        }
    }
}
