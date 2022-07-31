using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts.TimeSeries;
using Area52.Services.Implementation.Mongo.Models;
using Area52.Services.Implementation.Raven.TimeSeries;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo.TimeSeries;

internal class TimeSeriesWriter : ITimeSeriesWriter
{
    private readonly List<MongoTimeSerieItem> buffer;
    private readonly IMongoCollection<MongoTimeSerieItem> collection;
    private readonly MongoDB.Bson.ObjectId definitionId;
    private readonly ILogger<TimeSeriesWriter> logger;

    public TimeSeriesWriter(IMongoCollection<MongoTimeSerieItem> collection, MongoDB.Bson.ObjectId definitionId, ILogger<TimeSeriesWriter> logger)
    {
        this.buffer = new List<MongoTimeSerieItem>(100);
        this.collection = collection;
        this.definitionId = definitionId;
        this.logger = logger;
    }

    public ValueTask Write(DateTimeOffset timestamp, double value, string? tag)
    {
        MongoTimeSerieItem item = new MongoTimeSerieItem()
        {
            Timestamp = timestamp.UtcDateTime,
            Value = value
        };
        item.Meta.Tag = tag;
        item.Meta.DefinitionId = this.definitionId;

        this.buffer.Add(item);

        if (this.buffer.Count >= 100)
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

        await this.collection.InsertManyAsync(this.buffer);

        this.logger.LogInformation("Writing to timeserie in definition {timeSerieDefinitionId} {count} entities.", this.definitionId, this.buffer.Count);
        this.buffer.Clear();
    }
}
