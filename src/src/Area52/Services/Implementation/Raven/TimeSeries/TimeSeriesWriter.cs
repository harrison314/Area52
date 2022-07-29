using Area52.Services.Contracts;
using Raven.Client.Documents;

namespace Area52.Services.Implementation.Raven.TimeSeries;

internal class TimeSeriesWriter : ITimeSeriesWriter
{
    private record struct TsEntity(DateTimeOffset timestamp, double value, string? tag);

    private readonly List<TsEntity> buffer;
    private readonly IDocumentStore documentStore;
    private readonly string documentId;
    private readonly ILogger<TimeSeriesWriter> logger;

    public TimeSeriesWriter(IDocumentStore documentStore, string documentId, ILogger<TimeSeriesWriter> logger)
    {
        this.buffer = new List<TsEntity>(TimeSeriesConstants.TsFlushCount);
        this.documentStore = documentStore;
        this.documentId = documentId;
        this.logger = logger;
    }

    public ValueTask Write(DateTimeOffset timestamp, double value, string? tag)
    {
        this.buffer.Add(new TsEntity(timestamp, value, tag));

        if (this.buffer.Count >= TimeSeriesConstants.TsFlushCount)
        {
            return this.FlushBuffer();
        }

        return new ValueTask();
    }

    public async ValueTask DisposeAsync()
    {
        this.logger.LogTrace("Entering to DisposeAsync");

        if (this.buffer.Count > 0)
        {
            await this.FlushBuffer();
        }
    }

    private async ValueTask FlushBuffer()
    {
        this.logger.LogTrace("Entering to FlushBuffer");
        using var session = this.documentStore.OpenAsyncSession();

        global::Raven.Client.Documents.Session.IAsyncSessionDocumentTimeSeries ts = session.TimeSeriesFor(this.documentId, TimeSeriesConstants.TsName);

        foreach (var entity in this.buffer)
        {
            ts.Append(entity.timestamp.UtcDateTime, new double[] { entity.value }, entity.tag);
        }

        await session.SaveChangesAsync();
        this.logger.LogInformation("Writing to timeserie in definition {timeSerieDefinitionId} {count} entities.", this.documentId, this.buffer.Count);
        this.buffer.Clear();
    }
}
