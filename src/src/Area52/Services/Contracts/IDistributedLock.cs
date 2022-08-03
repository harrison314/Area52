namespace Area52.Services.Contracts;

public interface IDistributedLock : IAsyncDisposable
{
    bool Acquired
    {
        get;
    }
}
