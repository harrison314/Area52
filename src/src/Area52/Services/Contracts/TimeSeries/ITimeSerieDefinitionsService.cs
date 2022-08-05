namespace Area52.Services.Contracts.TimeSeries;

public interface ITimeSerieDefinitionsService
{
    Task<string> Create(TimeSerieDefinition timeSerieDefinition);

    Task<IReadOnlyList<TimeSerieDefinitionInfo>> FindDefinitions();

    Task<TimeSerieDefinition> FindById(string id);

    void CheckQuery(string query);
}
