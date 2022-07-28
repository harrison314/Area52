namespace Area52.Services.Contracts;

public interface ITimeSerieDefinitionsService
{
    Task<string> Create(TimeSerieDefinition timeSerieDefinition);

    Task<IReadOnlyList<TimeSerieDefinitionInfo>> FindDefinictions();

    Task<TimeSerieDefinition> FindById(string id);

    void CheckQuery(string query);
}
