using Area52.Services.Contracts;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.IdGenerators;

namespace Area52.Services.Implementation.Mongo.Models
{
    public class MongoApiKeyModel
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

        public MongoApiKeyModel()
        {
            this.Version = 1;
            this.Name = string.Empty;
            this.Key = string.Empty;
            this.Metadata = default!;
        }
    }
}
