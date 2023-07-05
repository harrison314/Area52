using Area52.Services.Contracts;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace Area52.Services.Implementation.Mongo.Models
{
    public class MongoApiKeySettingsModel
    {
        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id
        {
            get;
            set;
        }

        [BsonElement("v")]
        public int Version
        {
            get;
            set;
        }

        public bool IsEnabled
        {
            get;
            set;
        }

        public UserObjectMetadata Metadata
        {
            get;
            set;
        }

        public MongoApiKeySettingsModel()
        {
            this.Version = 1;
            this.Metadata = default!;
        }
    }
}
