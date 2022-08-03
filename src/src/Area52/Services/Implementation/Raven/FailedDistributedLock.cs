using Area52.Services.Contracts;

namespace Area52.Services.Implementation.Raven;

internal class FailedDistributedLock : IDistributedLock
{
    public bool Acquired
    {
        get => false;
    }

    public FailedDistributedLock()
    {

    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask();
    }
}