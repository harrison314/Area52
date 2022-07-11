namespace Area52.Services.Contracts;

public interface IDistributedLocker
{
    Task<IDistributedLock> TryAquire(string name, TimeSpan resrvedTime, CancellationToken cancellationToken = default);
}
