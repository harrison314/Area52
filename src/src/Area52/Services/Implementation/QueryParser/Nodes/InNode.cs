namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class InNode : BinaryOpNode
{
    public InNode(IAstNode left, IAstNode righht) : base(left, righht)
    {
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        this.Left.ToRql(context);
        context.Append(" IN (");
        this.Righht.ToRql(context);
        context.Append(")");
    }

    public override string ToString()
    {
        return this.ToString("in");
    }
}
