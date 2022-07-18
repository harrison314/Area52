namespace Area52.Services.Contracts;

public interface ILogManager
{
    Task RemoveOldLogs(DateTimeOffset timeAtDeteledLogs, CancellationToken cancellationToken);
}
