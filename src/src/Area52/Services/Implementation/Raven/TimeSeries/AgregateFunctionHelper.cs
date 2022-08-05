using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts.TimeSeries;
using Raven.Client.Documents.Queries.TimeSeries;

namespace Area52.Services.Implementation.Raven.TimeSeries;


internal record struct AggregateFunctionRuntime(Expression<Func<ITimeSeriesGrouping, object>> GroupByExpression, Func<TimeSeriesAggregationResult, List<TimeSeriesItem>> Mapper);
internal static class AggregateFunctionHelper
{
    public static AggregateFunctionRuntime GetFunctions(AggregateFn aggregateFunction)
    {
        return aggregateFunction switch
        {
            AggregateFn.Count => new AggregateFunctionRuntime(t => new { Count = t.Count() }, MapCount),
            AggregateFn.Sum => new AggregateFunctionRuntime(t => new { Sum = t.Sum() }, MapSum),
            AggregateFn.Min => new AggregateFunctionRuntime(t => new { Min = t.Min() }, MapMin),
            AggregateFn.Max => new AggregateFunctionRuntime(t => new { Max = t.Max() }, MapMax),
            AggregateFn.Avg => new AggregateFunctionRuntime(t => new { Average = t.Average() }, MapAvg),
            _ => throw new InvalidProgramException($"Enum value {aggregateFunction} is not supported.")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DateTime GetAvgTime(ref TimeSeriesRangeAggregation aggregation)
    {
        long from = aggregation.From.Ticks;
        long to = aggregation.To.Ticks;
        return new DateTime(from + (to - from) / 2, DateTimeKind.Utc);
    }

    private static List<TimeSeriesItem> MapCount(TimeSeriesAggregationResult ts)
    {
        List<TimeSeriesItem> result = new List<TimeSeriesItem>(ts.Results.Length);
        for (int i = 0; i < ts.Results.Length; i++)
        {
            result.Add(new TimeSeriesItem()
            {
                Time = GetAvgTime(ref ts.Results[i]),
                Value = ts.Results[i].Count[0]
            });
        }

        return result;
    }

    private static List<TimeSeriesItem> MapSum(TimeSeriesAggregationResult ts)
    {
        List<TimeSeriesItem> result = new List<TimeSeriesItem>(ts.Results.Length);
        for (int i = 0; i < ts.Results.Length; i++)
        {
            result.Add(new TimeSeriesItem()
            {
                Time = GetAvgTime(ref ts.Results[i]),
                Value = ts.Results[i].Sum[0]
            });
        }

        return result;
    }

    private static List<TimeSeriesItem> MapMin(TimeSeriesAggregationResult ts)
    {
        List<TimeSeriesItem> result = new List<TimeSeriesItem>(ts.Results.Length);
        for (int i = 0; i < ts.Results.Length; i++)
        {
            result.Add(new TimeSeriesItem()
            {
                Time = GetAvgTime(ref ts.Results[i]),
                Value = ts.Results[i].Min[0]
            });
        }

        return result;
    }

    private static List<TimeSeriesItem> MapMax(TimeSeriesAggregationResult ts)
    {
        List<TimeSeriesItem> result = new List<TimeSeriesItem>(ts.Results.Length);
        for (int i = 0; i < ts.Results.Length; i++)
        {
            result.Add(new TimeSeriesItem()
            {
                Time = GetAvgTime(ref ts.Results[i]),
                Value = ts.Results[i].Max[0]
            });
        }

        return result;
    }

    private static List<TimeSeriesItem> MapAvg(TimeSeriesAggregationResult ts)
    {
        List<TimeSeriesItem> result = new List<TimeSeriesItem>(ts.Results.Length);
        for (int i = 0; i < ts.Results.Length; i++)
        {
            result.Add(new TimeSeriesItem()
            {
                Time = GetAvgTime(ref ts.Results[i]),
                Value = ts.Results[i].Average[0]
            });
        }

        return result;
    }
}
