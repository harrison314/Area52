namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class SearchNode : BinaryOpNode
{
    public SearchNode(IAstNode left, IAstNode righht) : base(left, righht)
    {
    }

    public SearchNode(IAstNode str)
        : base(new NullValueNode(), str)
    {
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        context.Append("search(");
        if (this.Left is NullValueNode)
        {
            context.Append(context.FulltextPropertyName);
        }
        else
        {
            this.Left.ToRql(context);
        }

        context.Append(", ");
        this.Righht.ToRql(context);
        context.Append(")");
    }

    public override string ToString()
    {
        if (this.Left is NullValueNode)
        {
            return this.Righht.ToString()!;
        }

        return this.ToString("serach");
    }
}
