namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class LogIdNode : IAstNode
{
    public StringValueNode Id
    {
        get;
        set;
    }

    public LogIdNode(StringValueNode id)
    {
        this.Id = id;
    }

    public void ToRql(RqlQueryBuilderContext context)
    {
        context.Append("id() == ");
        this.Id.ToRql(context);
    }

    public override string ToString()
    {
        return $"logid({this.Id})";
    }
}
