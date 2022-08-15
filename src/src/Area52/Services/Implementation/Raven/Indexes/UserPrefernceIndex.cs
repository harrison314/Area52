using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Implementation.Raven.Models;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Indexes.TimeSeries;

namespace Area52.Services.Implementation.Raven.Indexes;

public class UserPrefernceIndex : AbstractIndexCreationTask<UserPrefernce, UserPrefernceIndex.Result>
{
    public class Result
    {
        public string UserId
        {
            get;
            set;
        }

        public Result()
        {
            this.UserId = null!;
        }
    }

    public override string IndexName
    {
        get => "UserPrefernce/MainIndex";
    }

    public UserPrefernceIndex()
    {
        this.Map = userPrefernces => from up in userPrefernces
                                     select new Result()
                                     {
                                         UserId = up.Metadata.CreatedById
                                     };
    }
}
