using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Area52.Ufo.Services.Configuration;
using Area52.Ufo.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Area52.Ufo.Services.Implementation;

public class BatchClefQeueu : IBatchClefQeueu
{
    private readonly AsyncEvent waiter;
    private ConcurrentBag<string> lines;

    public BatchClefQeueu(IOptions<UfoSetup> ufoSetup)
    {
        this.lines = new ConcurrentBag<string>();
        this.waiter = new AsyncEvent(ufoSetup.Value.RecomandetBatchSize, TimeSpan.FromSeconds(2.0));
    }

    public void Insert(ClefObject clefObject)
    {
        string line = System.Text.Json.JsonSerializer.Serialize<ClefObject>(clefObject,
                  SourceGenerationContext.Default.ClefObject);

        this.lines.Add(line);
        this.waiter.Set(1);
    }

    public void Insert(string lines, int count)
    {
        this.lines.Add(lines);
        this.waiter.Set(count);
    }

    public async Task<bool> Wait(CancellationToken cancellationToken)
    {
        await this.waiter.Wait(cancellationToken);
        return this.lines.Count > 0;
    }

    public string GetContent()
    {
        StringBuilder sb = new StringBuilder();
        while (this.lines.TryTake(out string? lines))
        {
            sb.Append(lines.AsSpan().TrimEnd());
            sb.AppendLine();
        }

        this.waiter.Reset();

        return sb.ToString();
    }
}
