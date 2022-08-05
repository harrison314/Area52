namespace Area52.Services.Contracts;

public interface IDistributedLocker
{
    Task<IDistributedLock> TryAcquire(string name, TimeSpan reservedTime, CancellationToken cancellationToken = default);
}
