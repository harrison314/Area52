namespace Area52.Services.Contracts;

public interface IDistributedLock : IAsyncDisposable
{
    bool Aquired
    {
        get;
    }
}
