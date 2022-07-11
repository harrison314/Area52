namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class AndNode : BinaryOpNode
{
    public AndNode(IAstNode left, IAstNode righht) : base(left, righht)
    {
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        context.Append('(');
        this.Left.ToRql(context);
        context.Append(") and (");
        this.Righht.ToRql(context);
        context.Append(')');
    }

    public override string ToString()
    {
        return this.ToString("and");
    }
}
