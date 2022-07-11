namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class GtOrEqNode : BinaryOpNode
{
    public GtOrEqNode(IAstNode left, IAstNode righht) : base(left, righht)
    {
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        this.Left.ToRql(context);
        context.Append(" >= ");
        this.Righht.ToRql(context);
    }

    public override string ToString()
    {
        return this.ToString(">=");
    }
}
