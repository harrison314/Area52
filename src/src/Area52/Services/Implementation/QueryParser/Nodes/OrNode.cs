namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class OrNode : BinaryOpNode
{
    public OrNode(IAstNode left, IAstNode right) : base(left, right)
    {
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        context.Append('(');
        this.Left.ToRql(context);
        context.Append(") or (");
        this.Right.ToRql(context);
        context.Append(')');
    }

    public override string ToString()
    {
        return this.ToString("or");
    }
}
