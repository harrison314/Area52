using Area52.Services.Contracts.TimeSeries;
using Raven.Client.Documents.Indexes;

namespace Area52.Services.Implementation.Raven.Indexes;

public class TimeSerieDefinitionIndex : AbstractIndexCreationTask<TimeSerieDefinition, TimeSerieDefinitionIndex.Result>
{
    public class Result
    {
        public string Id
        {
            get;
            set;
        }

        public DateTime Created
        {
            get;
            set;
        }

        public DateTime? LastExecute
        {
            get;
            set;
        }
    }

    public TimeSerieDefinitionIndex()
    {
        this.Map = definitions => from d in definitions
                                  where d.Enabled
                                  select new Result()
                                  {
                                      Id = d.Id,
                                      Created = d.Metadata.Created,
                                      LastExecute = d.LastExecutionInfo == null ? null : d.LastExecutionInfo.LastExecute
                                  };
    }
}
