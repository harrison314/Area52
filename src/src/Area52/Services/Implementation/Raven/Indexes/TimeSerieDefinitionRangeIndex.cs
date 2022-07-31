using Area52.Services.Contracts.TimeSeries;
using Raven.Client.Documents.Indexes.TimeSeries;

namespace Area52.Services.Implementation.Raven.Indexes;

public class TimeSerieDefinitionRangeIndex : AbstractTimeSeriesIndexCreationTask<TimeSerieDefinition, TimeSerieDefinitionRangeIndex.Result>
{
    public class Result
    {
        public string DocumentId
        {
            get;
            set;
        }

        public DateTime Start
        {
            get;
            set;
        }

        public DateTime End
        {
            get;
            set;
        }

        public long Count
        {
            get;
            set;
        }

        public Result()
        {
            this.DocumentId = string.Empty;
        }
    }

    public override string IndexName
    {
        get => "TimeSerieDefinition/Range";
    }

    public TimeSerieDefinitionRangeIndex()
    {
        this.AddMap(TimeSeries.TimeSeriesConstants.TsName,
            timeSeries => from ts in timeSeries
                          select new Result()
                          {
                              DocumentId = ts.DocumentId,
                              Start = ts.Start,
                              End = ts.End,
                              Count = ts.Count
                          });

        this.Reduce = results => from result in results
                                 group result by result.DocumentId into r
                                 select new Result()
                                 {
                                     DocumentId = r.Key,
                                     Start = r.Min(t => t.Start),
                                     End = r.Max(t => t.End),
                                     Count = r.Sum(t => t.Count)
                                 };

        this.Store(t => t.Start, global::Raven.Client.Documents.Indexes.FieldStorage.Yes);
        this.Store(t => t.End, global::Raven.Client.Documents.Indexes.FieldStorage.Yes);
        this.Store(t => t.Count, global::Raven.Client.Documents.Indexes.FieldStorage.Yes);
    }
}
