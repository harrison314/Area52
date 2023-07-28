using Area52.Services.Configuration;
using Area52.Services.Contracts;
using Area52.Services.Contracts.TimeSeries;
using Area52.Services.Implementation;
using Area52.Services.Implementation.QueryParser;
using Area52.Services.Implementation.QueryParser.Nodes;
using Microsoft.Extensions.Options;

namespace Area52.Infrastructure.HostedServices;

public class TimeSeriesBackgroundService : BackgroundService
{
    private readonly IDistributedLocker locker;
    private readonly ITimeSeriesService timeSeriesService;
    private readonly ILogReader logReader;
    private readonly IOptions<TimeSeriesSetup> timeSeriesSetup;
    private readonly ILogger<TimeSeriesBackgroundService> logger;

    public TimeSeriesBackgroundService(IDistributedLocker locker,
        ITimeSeriesService timeSeriesService,
        ILogReader logReader,
        IOptions<TimeSeriesSetup> timeSeriesSetup,
        ILogger<TimeSeriesBackgroundService> logger)
    {
        this.locker = locker;
        this.timeSeriesService = timeSeriesService;
        this.logReader = logReader;
        this.timeSeriesSetup = timeSeriesSetup;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogTrace("Entering to ExecuteAsync.");

        TimeSeriesSetup setup = this.timeSeriesSetup.Value;
        await Task.Delay(setup.StartupDelay, stoppingToken);

        PeriodicTimer timer = new PeriodicTimer(setup.CheckNewDataInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime execBefore = DateTime.UtcNow - setup.FindDataBeforeInterval;
            IReadOnlyList<TimeSeriesDefinitionId> tsDefinitionsFoExec = await this.timeSeriesService.GetDefinitionForExecute(execBefore, stoppingToken);
            this.logger.LogTrace("Found {count} candidates executed before {execBefore}.", tsDefinitionsFoExec.Count, execBefore);

            foreach (TimeSeriesDefinitionId id in tsDefinitionsFoExec)
            {
                await using IDistributedLock dLock = await this.locker.TryAcquire(id.Id, TimeSpan.FromSeconds(30.0), stoppingToken);
                if (dLock.Acquired)
                {
                    TimeSeriesDefinitionUnit tsUnit = await this.timeSeriesService.GetDefinitionUnit(id.Id, stoppingToken);
                    if (!this.CheckExecutionTime(tsUnit, execBefore))
                    {
                        this.logger.LogDebug("Time series definition with id {timeSeriesDefinitionId} has updated in another instance.", id.Id);
                        continue;
                    }

                    await this.ExecuteDefinition(tsUnit, stoppingToken);
                }
                else
                {
                    this.logger.LogDebug("Lock for time series definition with id {timeSeriesDefinitionId} not acquired lock.", id.Id);
                }
            }

            await timer.WaitForNextTickAsync(stoppingToken);
            if (setup.RemoveOldData)
            {
                await this.RemoveOldData(TimeSpan.FromDays(setup.RemoveDataAdDaysOld), stoppingToken);
            }
        }
    }

    private async Task ExecuteDefinition(TimeSeriesDefinitionUnit tsUnit, CancellationToken stoppingToken)
    {
        this.logger.LogTrace("Entering to ExecuteDefinition for time series definition {timeSeriesDefinitionId}.", tsUnit.Id);
        DateTime timeTo = DateTime.UtcNow;

        IAstNode astTree = Parser.SimpleParse(tsUnit.Query);
        IAstNode restrictedAstTree = this.AddTimestampToQuery(astTree, tsUnit.LastExecuted, timeTo);

        await using (ITimeSeriesWriter writer = await this.timeSeriesService.CreateWriter(tsUnit.Id, stoppingToken))
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

        return logEntity.Properties.Where(t => t.Name == valueFieldName)
              .Select(t =>
              {
                  if (t.Valued.HasValue) return t.Valued.Value;
                  if (t.Values != null && double.TryParse(t.Values, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result)) return result;
                  return 1.0;
              })
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
        PropertyNode timestampNode = new PropertyNode(nameof(LogEntity.Timestamp));
        if (from.HasValue)
        {
            GtOrEqNode fromNode = new GtOrEqNode(timestampNode, new StringValueNode(from.Value.ToString(FormatConstants.SortableDateTimeFormat)));
            LtNode toNode = new LtNode(timestampNode, new StringValueNode(to.ToString(FormatConstants.SortableDateTimeFormat)));
            timeCondition = new AndNode(fromNode, toNode);
        }
        else
        {
            timeCondition = new LtNode(timestampNode, new StringValueNode(to.ToString(FormatConstants.SortableDateTimeFormat)));
        }

        return new AndNode(timeCondition, query);
    }

    private async Task RemoveOldData(TimeSpan timeSpan, CancellationToken stoppingToken)
    {
        await this.timeSeriesService.DeleteOldData(DateTime.UtcNow - timeSpan, stoppingToken);
    }
}
