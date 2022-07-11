namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class OrNode : BinaryOpNode
{
    public OrNode(IAstNode left, IAstNode righht) : base(left, righht)
    {
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        context.Append('(');
        this.Left.ToRql(context);
        context.Append(") or (");
        this.Righht.ToRql(context);
        context.Append(')');
    }

    public override string ToString()
    {
        return this.ToString("or");
    }
}
