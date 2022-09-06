using System;
using System.Threading;
using System.Threading.Tasks;

namespace Area52.Ufo.Services.Implementation;

internal class AsyncEvent
{
    private long actualCount;
    private long limit;
    private readonly TimeSpan timout;
    private DateTime nextSend;
    private readonly object syncRoot;

    public AsyncEvent(int limit, TimeSpan timout)
    {
        this.actualCount = 0;
        this.limit = limit;
        this.timout = timout;
        this.nextSend = DateTime.MaxValue;
        this.syncRoot = new object();
    }

    public async Task<bool> Wait(CancellationToken cancelationToken)
    {
        try
        {
            while (this.CanWait())
            {
                await Task.Delay(500, cancelationToken);
            }
        }
        catch (TaskCanceledException)
        {
            return false;
        }


        return true;
    }

    private bool CanWait()
    {
        lock (this.syncRoot)
        {
            return this.actualCount > this.limit || this.nextSend > DateTime.UtcNow;
        }
    }

    public void Reset()
    {
        lock (this.syncRoot)
        {
            this.nextSend = DateTime.MaxValue;
            this.actualCount = 0;
        }
    }

    public void Set(int count)
    {
        lock (this.syncRoot)
        {
            this.actualCount += count;
            if (this.nextSend == DateTime.MaxValue)
            {
                this.nextSend = DateTime.UtcNow + this.timout;
            }
        }
    }
}
