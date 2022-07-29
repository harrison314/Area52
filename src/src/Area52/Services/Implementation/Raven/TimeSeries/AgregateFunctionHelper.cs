using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Raven.Client.Documents.Queries.TimeSeries;

namespace Area52.Services.Implementation.Raven.TimeSeries;


internal record struct AgregateFunctionRuntime(Expression<Func<ITimeSeriesGrouping, object>> GroupbyExpression, Func<TimeSeriesAggregationResult, List<TimeSeriesItem>> Mapper);
internal static class AgregateFunctionHelper
{
    public static AgregateFunctionRuntime GetFunctions(AgregateFn agregateFunction)
    {
        return agregateFunction switch
        {
            AgregateFn.Count => new AgregateFunctionRuntime(t => new { Count = t.Count() }, MapCount),
            AgregateFn.Sum => new AgregateFunctionRuntime(t => new { Sum = t.Sum() }, MapSum),
            AgregateFn.Min => new AgregateFunctionRuntime(t => new { Min = t.Min() }, MapMin),
            AgregateFn.Max => new AgregateFunctionRuntime(t => new { Max = t.Max() }, MapMax),
            AgregateFn.Avg => new AgregateFunctionRuntime(t => new { Average = t.Average() }, MapAvg),
            _ => throw new InvalidProgramException($"Enum value {agregateFunction} is not supported.")
        };
    }

    private static List<TimeSeriesItem> MapCount(TimeSeriesAggregationResult ts)
    {
        List<TimeSeriesItem> result = new List<TimeSeriesItem>(ts.Results.Length);
        for (int i = 0; i < ts.Results.Length; i++)
        {
            result.Add(new TimeSeriesItem()
            {
                From = ts.Results[i].From,
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
                From = ts.Results[i].From,
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
                From = ts.Results[i].From,
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
                From = ts.Results[i].From,
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
                From = ts.Results[i].From,
                Value = ts.Results[i].Average[0]
            });
        }

        return result;
    }
}
