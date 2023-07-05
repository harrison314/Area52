using MongoDB.Bson;

namespace Area52.Services.Implementation.Mongo.QueryTranslator;

internal struct BsonCtxNode
{
    public QueryNodeType Type
    {
        get;
    }

    public BsonValue Value
    {
        get;
    }

    public BsonCtxNode(BsonValue value, QueryNodeType type)
    {
        this.Type = type;
        this.Value = value;
    }

    public override string ToString()
    {
        return $"{this.Type} -> {this.Value}";
    }
}
