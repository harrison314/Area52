﻿using Area52.Services.Contracts;

namespace Area52.Services.Implementation.Mongo;

internal class FailedDistributedLock : IDistributedLock
{
    public bool Aquired
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
