using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Implementation.Mongo.Models;

public static class CollectionNames
{
    public const string LockAcquires = "LockAcquires";
    public const string LogEntities = "LogEntities";
    public const string DataProtectionKeys = "DataProtectionKey";
    public const string MongoTimeSeriesDefinition = "TimeSeriesDefinition";
    public const string MongoTimeSeriesItems = "TimeSeriesItems";
    public const string MongoUserPrefernce = "UserPrefernce";
}
