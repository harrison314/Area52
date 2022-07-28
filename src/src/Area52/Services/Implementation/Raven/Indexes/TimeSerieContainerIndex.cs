using Area52.Services.Implementation.Raven.Models;
using Raven.Client.Documents.Indexes;

namespace Area52.Services.Implementation.Raven.Indexes;

public class TimeSerieContainerIndex : AbstractIndexCreationTask<TimeSerieContainer, TimeSerieContainerIndex.Result>
{
    public class Result
    {
        public string TimeSerieDefinitionId
        {
            get;
            set;
        }
    }

    public TimeSerieContainerIndex()
    {
        this.Map = container => from c in container
                                select new Result()
                                {
                                    TimeSerieDefinitionId = c.TimeSerieDefinitionId
                                };
    }
}
