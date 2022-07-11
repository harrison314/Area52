namespace Area52.Services.Contracts;

public class ReadLastLogResult
{
    public IReadOnlyList<LogInfo> Logs
    {
        get;
    }

    public long TotalResults
    {
        get;
    }

    public ReadLastLogResult(IReadOnlyList<LogInfo> logs, long totalResults)
    {
        this.Logs = logs;
        this.TotalResults = totalResults;
    }
}
