namespace Area52.Services.Contracts.TimeSeries;

public interface ITimeSeriesWriter : IAsyncDisposable
{
    ValueTask Write(DateTimeOffset timestamp, double value, string? tag);
}
