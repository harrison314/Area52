using Area52.Services.Contracts;
using Area52.Services.Implementation.QueryParser;
using Area52.Services.Implementation.QueryParser.Nodes;

namespace Area52.Infrastructure.HostedServices;

public class TimeSeriesBackgroundService : BackgroundService
{
    private readonly IDistributedLocker locker;
    private readonly ITimeSeriesService timeSeriesService;
    private readonly ILogReader logReader;
    private readonly ILogger<TimeSeriesBackgroundService> logger;

    public TimeSeriesBackgroundService(IDistributedLocker locker,
        ITimeSeriesService timeSeriesService,
        ILogReader logReader,
        ILogger<TimeSeriesBackgroundService> logger)
    {
        this.locker = locker;
        this.timeSeriesService = timeSeriesService;
        this.logReader = logReader;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogTrace("Entering to ExecuteAsync.");

        await Task.Delay(15 * 1000); //TODO

        PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(60)); // TODO: konfiguracia
        while (!stoppingToken.IsCancellationRequested)
        {

            DateTime execBefore = DateTime.UtcNow - TimeSpan.FromMinutes(2.0); //TODO: konfiguracia
            IReadOnlyList<TimeSeriesDefinitionId> tsDefinitionsFoExec = await this.timeSeriesService.GetDefinitionForExecute(execBefore, stoppingToken);
            this.logger.LogTrace("Found {count} candidates executed before {execBefore}.", tsDefinitionsFoExec.Count, execBefore);

            foreach (TimeSeriesDefinitionId id in tsDefinitionsFoExec)
            {
                await using IDistributedLock dLock = await this.locker.TryAquire(id.Id, TimeSpan.FromSeconds(30.0), stoppingToken);
                if (dLock.Aquired)
                {
                    TimeSeriesDefinitionUnit tsUnit = await this.timeSeriesService.GetDefinitionUnit(id.Id, stoppingToken);
                    if (!this.CheckExecutionTime(tsUnit, execBefore))
                    {
                        this.logger.LogDebug("Time series definion with id {timeSeriesDefinitionId} has updaten in another instance.", id.Id);
                        continue;
                    }

                    await this.ExecuteDefinition(tsUnit, stoppingToken);
                }
                else
                {
                    this.logger.LogDebug("Lock for time series definition with id {timeSeriesDefinitionId} not aquired lock.", id.Id);
                }
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task ExecuteDefinition(TimeSeriesDefinitionUnit tsUnit, CancellationToken stoppingToken)
    {
        this.logger.LogTrace("Entering to ExecuteDefinition wor time serie definition {timeSeriesDefinitionId}.", tsUnit.Id);
        DateTime timeTo = DateTime.UtcNow;

        IAstNode astTree = Parser.SimpleParse(tsUnit.Query);
        IAstNode restrictedAstTree = this.AddTimestampToQuery(astTree, tsUnit.LastExecuted, timeTo);


        await using (ITimeSeriesWriter writer = await this.timeSeriesService.CreateWriter(tsUnit.Id,
                    tsUnit.Name,
                    stoppingToken))
        {
            await foreach (LogEntity logEntity in this.logReader.ReadLogs(restrictedAstTree, null).WithCancellation(stoppingToken))
            {


                double numericValue = this.GetNumericValue(tsUnit.ValueFieldName, logEntity);
                string? tagValue = this.GetTagValue(tsUnit.TagFieldName, logEntity);
                await writer.Write(logEntity.Timestamp, numericValue, tagValue);
            }
        }

        await this.timeSeriesService.ConfirmWriting(tsUnit.Id,
            new LastExecutionInfo()
            {
                LastExecute = timeTo
            }, stoppingToken);
    }

    private double GetNumericValue(string? valueFieldName, LogEntity logEntity)
    {
        if (string.IsNullOrEmpty(valueFieldName))
        {
            return 1.0;
        }

        return logEntity.Properties.Where(t => t.Name == valueFieldName && t.Valued.HasValue)
              .Select(t => t.Valued ?? 1.0)
              .FirstOrDefault(1.0);
    }

    private string? GetTagValue(string? tagFieldName, LogEntity logEntity)
    {
        if (string.IsNullOrEmpty(tagFieldName))
        {
            return null;
        }

        return tagFieldName switch
        {
            nameof(LogEntity.EventId) => logEntity.EventId?.ToString(),
            nameof(LogEntity.Exception) => logEntity.Exception?.ToString(),
            nameof(LogEntity.Level) => logEntity.Level.ToString(),
            nameof(LogEntity.LevelNumeric) => logEntity.LevelNumeric.ToString(),
            nameof(LogEntity.Message) => logEntity.Message,
            nameof(LogEntity.MessageTemplate) => logEntity.MessageTemplate,
            nameof(LogEntity.Timestamp) => logEntity.Timestamp.ToString("s"),
            _ => logEntity.Properties.Where(t => t.Name == tagFieldName)
              .Select(t => t.Values)
              .FirstOrDefault()
        };
    }

    private bool CheckExecutionTime(TimeSeriesDefinitionUnit tsUnit, DateTime execBefore)
    {
        if (tsUnit.LastExecuted.HasValue)
        {
            System.Diagnostics.Debug.Assert(tsUnit.LastExecuted.Value.Kind == DateTimeKind.Utc);
            System.Diagnostics.Debug.Assert(execBefore.Kind == DateTimeKind.Utc);

            return tsUnit.LastExecuted.Value < execBefore;
        }

        return true;
    }

    private IAstNode AddTimestampToQuery(IAstNode query, DateTime? from, DateTime to)
    {
        IAstNode timeCondition;
        PropertyNode timespanNode = new PropertyNode(nameof(LogEntity.Timestamp));
        if (from.HasValue)
        {
            GtOrEqNode fromNode = new GtOrEqNode(timespanNode, new StringValueNode(from.Value.ToString("s")));
            LtNode toNode = new LtNode(timespanNode, new StringValueNode(to.ToString("s")));
            timeCondition = new AndNode(fromNode, toNode);
        }
        else
        {
            timeCondition = new LtNode(timespanNode, new StringValueNode(to.ToString("s")));
        }

        return new AndNode(timeCondition, query);
    }
}

//internal sealed class TsWriterCache : IAsyncDisposable
//{
//    private Dictionary<string, ITimeSeriesWriter> writers;
//    public TsWriterCache()
//    {
//        this.writers = new Dictionary<string, ITimeSeriesWriter>();
//    }

//    public async ValueTask<ITimeSeriesWriter> GetOrCreate(ITimeSeriesService service,
//        string definitonId,
//        string definitionName,
//        CancellationToken cancellationToken)
//    {
//        if (this.writers.TryGetValue(definitonId, out ITimeSeriesWriter? writer))
//        {
//            return writer;
//        }

//        writer = await service.CreateWriter(definitonId, definitionName, name, cancellationToken);

//        this.writers.Add(name, writer);
//        return writer;
//    }

//    public async ValueTask DisposeAsync()
//    {
//        foreach (KeyValuePair<string, ITimeSeriesWriter> kvp in this.writers)
//        {
//            await kvp.Value.DisposeAsync();
//        }

//        this.writers.Clear();
//    }
//}
