namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class NullValueNode : IValueNode
{
    public NullValueNode()
    {
    }

    public void ToRql(RqlQueryBuilderContext context)
    {
        context.Append("null");
    }

    public override string ToString()
    {
        return "null";
    }
}
