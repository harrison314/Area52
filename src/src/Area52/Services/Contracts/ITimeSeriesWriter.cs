namespace Area52.Services.Contracts;

public interface ITimeSeriesWriter : IAsyncDisposable
{
    ValueTask Write(DateTimeOffset timestamp, double value, string? tag);
}
