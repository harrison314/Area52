namespace Area52.Services.Implementation.Raven;

public record QueryWithParameters(string Query, IReadOnlyDictionary<string, object> Parameters);