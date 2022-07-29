using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Contracts;

public interface ITimeSeriesService
{
    Task<IReadOnlyList<TimeSeriesDefinitionId>> GetDefinitionForExecute(DateTime executeBefore, CancellationToken cancellationToken);
    Task<TimeSeriesDefinitionUnit> GetDefinitionUnit(string id, CancellationToken cancellationToken);

    Task<ITimeSeriesWriter> CreateWriter(string definitionId, CancellationToken cancellationToken);

    Task ConfirmWriting(string definitionId, LastExecutionInfo lastExecutionInfo, CancellationToken cancellationToken);

    Task<IReadOnlyList<TimeSeriesItem>> ExecuteQuery(TimeSeriesQueryRequest request, CancellationToken cancellationToken);
}

public class TimeSeriesQueryRequest
{
    public string DefinitionId
    {
        get;
        init;
    }

    public DateTime From
    {
        get;
        init;
    }

    public DateTime To
    {
        get;
        init;
    }

    public TimeSeriesGroupByFn GroupByFunction
    {
        get;
        init;
    }
}

public enum TimeSeriesGroupByFn
{
    Seconds,
    Minutes,
    Hours,
    Days,
    Months,
    Quarters,
    Years
}

public class TimeSeriesItem
{
    public DateTime From
    {
        get;
        init;
    }

    public double Value
    {
        get;
        set;
    }
}
