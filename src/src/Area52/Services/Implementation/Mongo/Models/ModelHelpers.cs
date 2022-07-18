using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Area52.Services.Implementation.Mongo.Models;

internal static class ModelHelpers
{
    public static void RegisterMappings()
    {
        BsonClassMap.RegisterClassMap<LogInfo>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
            cm.MapIdMember(c => c.Id).SetSerializer(new StringSerializer(BsonType.ObjectId)).SetIgnoreIfDefault(true);
        });

    }
}
