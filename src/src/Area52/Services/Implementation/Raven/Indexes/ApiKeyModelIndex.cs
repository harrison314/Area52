using Area52.Services.Implementation.Raven.Models;
using Raven.Client.Documents.Indexes;

namespace Area52.Services.Implementation.Raven.Indexes;

public class ApiKeyModelIndex : AbstractIndexCreationTask<ApiKeyModel, UserPrefernceIndex.Result>
{
    public class Result
    {
        public string Name
        {
            get;
            set;
        }

        public string Key
        {
            get;
            set;
        }

        public DateTime Created
        {
            get;
            set;
        }

        public Result()
        {
            this.Name = null!;
            this.Key = null!;
        }
    }

    public override string IndexName
    {
        get => "ApiKeyModel/MainIndex";
    }

    public ApiKeyModelIndex()
    {
        this.Map = apiKeys => from ak in apiKeys
                                     where ak.IsEnabled
                                     select new Result()
                                     {
                                         Name = ak.Name, 
                                         Key = ak.Key,
                                         Created = ak.Metadata.Created
                                     };
    }
}
