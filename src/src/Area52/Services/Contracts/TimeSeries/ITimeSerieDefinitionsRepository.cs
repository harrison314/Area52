namespace Area52.Services.Contracts.TimeSeries;

public interface ITimeSerieDefinitionsRepository
{
    Task<string> Create(TimeSerieDefinition timeSerieDefinition);

    Task<IReadOnlyList<TimeSerieDefinitionInfo>> FindDefinictions();

    Task<TimeSerieDefinition> FindById(string id);

    Task Delete(string id);
}
