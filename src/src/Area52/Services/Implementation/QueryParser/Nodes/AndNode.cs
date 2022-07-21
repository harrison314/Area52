namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class AndNode : BinaryOpNode
{
    public AndNode(IAstNode left, IAstNode right) : base(left, right)
    {
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        context.Append('(');
        this.Left.ToRql(context);
        context.Append(") and (");
        this.Right.ToRql(context);
        context.Append(')');
    }

    public override string ToString()
    {
        return this.ToString("and");
    }
}
