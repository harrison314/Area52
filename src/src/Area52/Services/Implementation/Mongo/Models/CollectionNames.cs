using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Implementation.Mongo.Models;

public static class CollectionNames
{
    public const string LockAcquires = "LockAcquires";
    public const string LogEntitys = "LogEntitys";
    public const string DataProtectionKeys = "DataProtectionKey";
    public const string MongoTimeSerieDefinition = "TimeSerieDefinition";
    public const string MongoTimeSerieItems = "TimeSerieItems";
}
