namespace Area52.Services.Contracts;

public interface ILogDistributionServices
{
    Task<LogsDistribution> GetLogsDistribution(string query);
}
